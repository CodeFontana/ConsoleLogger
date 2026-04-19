using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace ConsoleLoggerLibrary;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("ConsoleLogger")]
internal sealed class ConsoleLoggerProvider : ILoggerProvider, IDisposable
{
    private const int DefaultQueueCapacity = 1024;

    private readonly ConcurrentDictionary<string, ConsoleLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly BlockingCollection<LogMessage> _messageQueue = new(DefaultQueueCapacity);
    private readonly Task _processMessages;
    private long _droppedMessageCount;

    public LogLevel LogMinLevel { get; private set; } = LogLevel.Trace;
    public bool UseUtcTimestamp { get; set; } = false;
    public bool MultiLineFormat { get; set; } = false;
    public bool IndentMultilineMessages { get; set; } = true;
    public bool EnableConsoleColors { get; set; } = true;
    public Func<LogMessage, string>? LogEntryFormatter { get; set; }

    /// <summary>
    /// Number of messages that were dropped because the queue was full at
    /// the time of enqueue. Useful for diagnostics under bursty load.
    /// </summary>
    public long DroppedMessageCount => Interlocked.Read(ref _droppedMessageCount);

    public Dictionary<LogLevel, ConsoleColor> LogLevelColors { get; set; } = new()
    {
        [LogLevel.Trace] = ConsoleColor.Cyan,
        [LogLevel.Debug] = ConsoleColor.Blue,
        [LogLevel.Information] = ConsoleColor.Green,
        [LogLevel.Warning] = ConsoleColor.Yellow,
        [LogLevel.Error] = ConsoleColor.Red,
        [LogLevel.Critical] = ConsoleColor.DarkRed,
        [LogLevel.None] = ConsoleColor.White
    };

    public ConsoleLoggerProvider(LogLevel logMinLevel = LogLevel.Trace,
                                 bool useUtcTimestamp = false,
                                 bool multiLineFormat = false,
                                 bool indentMultilineMessages = true,
                                 bool enableConsoleColors = true,
                                 Func<LogMessage, string>? logEntryFormatter = null) : this(new()
                                 {
                                     LogMinLevel = logMinLevel,
                                     UseUtcTimestamp = useUtcTimestamp,
                                     MultiLineFormat = multiLineFormat,
                                     IndentMultilineMessages = indentMultilineMessages,
                                     EnableConsoleColors = enableConsoleColors,
                                     LogEntryFormatter = logEntryFormatter
                                 })
    {

    }

    public ConsoleLoggerProvider(ConsoleLoggerOptions options)
    {
        LogMinLevel = options.LogMinLevel;
        UseUtcTimestamp = options.UseUtcTimestamp;
        MultiLineFormat = options.MultiLineFormat;
        IndentMultilineMessages = options.IndentMultilineMessages;
        EnableConsoleColors = options.EnableConsoleColors;
        LogLevelColors = options.LogLevelColors;
        LogEntryFormatter = options.LogEntryFormatter;
        _processMessages = Task.Factory.StartNew(DequeueMessages, this, TaskCreationOptions.LongRunning);
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

    private void WriteSingleLineFormatMessage(LogMessage message)
    {
        if (EnableConsoleColors == false)
        {
            string body = IndentMultilineMessages ? message.PaddedMessage : message.Message;
            Console.WriteLine($"{message.Header}{body}");
            return;
        }

        ConsoleColor originalColor = Console.ForegroundColor;
        ConsoleColor levelColor = GetLevelColor(message.LogLevel, originalColor);

        try
        {
            Console.Write($"{message.TimeStamp}|");
            Console.ForegroundColor = levelColor;
            Console.Write(LogMessage.LogLevelToString(message.LogLevel));
            Console.ForegroundColor = originalColor;
            Console.Write($"|{message.CategoryName}|");
            Console.ForegroundColor = levelColor;
            Console.WriteLine(IndentMultilineMessages ? message.PaddedMessage : message.Message);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    private void WriteMultiLineFormatMessage(LogMessage message)
    {
        if (EnableConsoleColors == false)
        {
            Console.Write($"[{message.TimeStamp}|");
            Console.Write(LogMessage.LogLevelToString(message.LogLevel));
            Console.Write($"|{message.CategoryName}]{Environment.NewLine}");
            Console.WriteLine($"{message.Message}{Environment.NewLine}");
            return;
        }

        ConsoleColor originalColor = Console.ForegroundColor;
        ConsoleColor levelColor = GetLevelColor(message.LogLevel, originalColor);

        try
        {
            Console.Write($"[{message.TimeStamp}|");
            Console.ForegroundColor = levelColor;
            Console.Write(LogMessage.LogLevelToString(message.LogLevel));
            Console.ForegroundColor = originalColor;
            Console.Write($"|{message.CategoryName}]{Environment.NewLine}");
            Console.ForegroundColor = levelColor;
            Console.WriteLine($"{message.Message}{Environment.NewLine}");
        }
        finally
        {
            Console.ForegroundColor = originalColor;
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
