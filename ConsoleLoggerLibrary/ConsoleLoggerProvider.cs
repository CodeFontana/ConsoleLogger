using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsoleLoggerLibrary;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("ConsoleLogger")]
internal sealed class ConsoleLoggerProvider : ILoggerProvider, IDisposable
{
    private const int DefaultQueueCapacity = 1024;

    private readonly ConcurrentDictionary<string, ConsoleLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly BlockingCollection<LogMessage> _messageQueue = new(DefaultQueueCapacity);
    private readonly Task _processMessages;
    private readonly IDisposable? _onChangeRegistration;
    private long _droppedMessageCount;

    public LogLevel LogMinLevel { get; private set; } = LogLevel.Trace;
    public bool UseUtcTimestamp { get; private set; }
    public bool MultiLineFormat { get; private set; }
    public bool IndentMultilineMessages { get; private set; } = true;
    public bool EnableConsoleColors { get; private set; } = true;
    public Func<LogMessage, string>? LogEntryFormatter { get; private set; }

    /// <summary>
    /// Number of messages that were dropped because the queue was full at
    /// the time of enqueue. Useful for diagnostics under bursty load.
    /// </summary>
    public long DroppedMessageCount => Interlocked.Read(ref _droppedMessageCount);

    /// <summary>
    /// Immutable fallback palette used when no LogLevelColors are supplied
    /// via options. FrozenDictionary gives optimal lookup performance for
    /// the dequeue-thread hot path.
    /// </summary>
    private static readonly FrozenDictionary<LogLevel, ConsoleColor> s_defaultLevelColors =
        new Dictionary<LogLevel, ConsoleColor>
        {
            [LogLevel.Trace] = ConsoleColor.Cyan,
            [LogLevel.Debug] = ConsoleColor.Blue,
            [LogLevel.Information] = ConsoleColor.Green,
            [LogLevel.Warning] = ConsoleColor.Yellow,
            [LogLevel.Error] = ConsoleColor.Red,
            [LogLevel.Critical] = ConsoleColor.DarkRed,
            [LogLevel.None] = ConsoleColor.White,
        }.ToFrozenDictionary();

    /// <summary>
    /// Immutable snapshot of the level-to-color map. Reassigned wholesale
    /// in <see cref="ApplyOptions"/> so the dequeue thread always observes
    /// a consistent instance — callers that mutate the source options
    /// dictionary cannot affect in-flight writes.
    /// </summary>
    public IReadOnlyDictionary<LogLevel, ConsoleColor> LogLevelColors { get; private set; } = s_defaultLevelColors;

    public ConsoleLoggerProvider(IOptionsMonitor<ConsoleLoggerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ApplyOptions(options.CurrentValue);
        _onChangeRegistration = options.OnChange(ApplyOptions);
        _processMessages = Task.Factory.StartNew(DequeueMessages, this, TaskCreationOptions.LongRunning);
    }

    private void ApplyOptions(ConsoleLoggerOptions options)
    {
        if (options is null)
        {
            return;
        }

        LogMinLevel = options.LogMinLevel;
        UseUtcTimestamp = options.UseUtcTimestamp;
        MultiLineFormat = options.MultiLineFormat;
        IndentMultilineMessages = options.IndentMultilineMessages;
        EnableConsoleColors = options.EnableConsoleColors;

        // Snapshot the caller-supplied dictionary so post-bind mutations on
        // the options instance cannot race with the dequeue thread.
        LogLevelColors = options.LogLevelColors is null
            ? s_defaultLevelColors
            : options.LogLevelColors.ToFrozenDictionary();

        LogEntryFormatter = options.LogEntryFormatter;
    }

    private static void DequeueMessages(object? state)
    {
        ConsoleLoggerProvider consoleLogger = (ConsoleLoggerProvider)state!;
        consoleLogger.DequeueMessages();
    }

    private void DequeueMessages()
    {
        foreach (LogMessage message in _messageQueue.GetConsumingEnumerable())
        {
            if (LogEntryFormatter != null)
            {
                Console.WriteLine(LogEntryFormatter(message));
            }
            else if (MultiLineFormat)
            {
                WriteMultiLineFormatMessage(message);
            }
            else
            {
                WriteSingleLineFormatMessage(message);
            }
        }
    }

    /// <summary>
    /// Serializes the multi-segment colored writes of a single log entry so
    /// concurrent providers or external Console writers cannot interleave
    /// between the prefix/level/middle/body runs and bleed colors.
    /// </summary>
    private static readonly object s_consoleSync = new();

    private void WriteSingleLineFormatMessage(LogMessage message)
    {
        string body = IndentMultilineMessages ? message.PaddedMessage : message.Message;

        if (EnableConsoleColors == false)
        {
            // Single atomic WriteLine: no color flips, no tearing window.
            Console.Out.WriteLine($"{message.Header}{body}");
            return;
        }

        // Build every segment up-front so no allocations occur inside the
        // color-flip critical section.
        string prefix = $"{message.TimeStamp}|";
        string levelText = LogMessage.LogLevelToString(message.LogLevel);
        string middle = message.EventIdText.Length > 0
            ? $"|{message.CategoryName}|{message.EventIdText}|"
            : $"|{message.CategoryName}|";

        WriteColoredLine(prefix, levelText, middle, body, GetLevelColor(message.LogLevel, ConsoleColor.Gray));
    }

    private void WriteMultiLineFormatMessage(LogMessage message)
    {
        string headerTail = message.EventIdText.Length > 0
            ? $"|{message.CategoryName}|{message.EventIdText}]"
            : $"|{message.CategoryName}]";

        if (EnableConsoleColors == false)
        {
            // Coalesce four Writes into a single atomic WriteLine.
            Console.Out.WriteLine(
                $"[{message.TimeStamp}|{LogMessage.LogLevelToString(message.LogLevel)}{headerTail}{Environment.NewLine}{message.Message}{Environment.NewLine}");
            return;
        }

        string prefix = $"[{message.TimeStamp}|";
        string levelText = LogMessage.LogLevelToString(message.LogLevel);
        string middle = $"{headerTail}{Environment.NewLine}";
        string body = $"{message.Message}{Environment.NewLine}";

        WriteColoredLine(prefix, levelText, middle, body, GetLevelColor(message.LogLevel, ConsoleColor.Gray));
    }

    /// <summary>
    /// Emits a four-segment colored line atomically: <paramref name="prefix"/>
    /// in the default color, <paramref name="levelText"/> and
    /// <paramref name="body"/> in <paramref name="levelColor"/>, and
    /// <paramref name="middle"/> in the default color. The whole sequence is
    /// held under a single console lock so concurrent writers cannot
    /// interleave between segments.
    /// </summary>
    private static void WriteColoredLine(string prefix, string levelText, string middle, string body, ConsoleColor levelColor)
    {
        lock (s_consoleSync)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            try
            {
                Console.Out.Write(prefix);
                Console.ForegroundColor = levelColor;
                Console.Out.Write(levelText);
                Console.ForegroundColor = originalColor;
                Console.Out.Write(middle);
                Console.ForegroundColor = levelColor;
                Console.Out.WriteLine(body);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }

    private ConsoleColor GetLevelColor(LogLevel logLevel, ConsoleColor fallback)
    {
        return LogLevelColors.TryGetValue(logLevel, out ConsoleColor color) ? color : fallback;
    }

    internal void EnqueueMessage(LogMessage message)
    {
        if (_messageQueue.IsAddingCompleted)
        {
            return;
        }

        try
        {
            // Non-blocking add: a logger should never block application threads
            // when its queue is saturated. Drop the message and increment the
            // dropped counter so callers can observe back-pressure.
            if (_messageQueue.TryAdd(message) == false)
            {
                Interlocked.Increment(ref _droppedMessageCount);
            }
        }
        catch (InvalidOperationException)
        {
            // Lost the race with CompleteAdding(); safe to ignore.
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
    }

    private ConsoleLogger CreateLoggerImplementation(string categoryName)
    {
        return new ConsoleLogger(this, categoryName);
    }

    public void Log(LogMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.LogLevel == LogLevel.None || message.LogLevel < LogMinLevel)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(message.Message))
        {
            return;
        }

        EnqueueMessage(message);
    }

    public void Dispose()
    {
        _onChangeRegistration?.Dispose();
        _messageQueue.CompleteAdding();

        try
        {
            _processMessages.Wait();
        }
        catch (TaskCanceledException) { }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException) { }

        _messageQueue.Dispose();
        _loggers.Clear();
    }
}
