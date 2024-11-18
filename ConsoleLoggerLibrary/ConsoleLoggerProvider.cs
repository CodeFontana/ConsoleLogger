using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace ConsoleLoggerLibrary;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("ConsoleLogger")]
internal sealed class ConsoleLoggerProvider : ILoggerProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, ConsoleLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly BlockingCollection<LogMessage> _messageQueue = new(1024);
    private readonly Task _processMessages;

    public LogLevel LogMinLevel { get; private set; } = LogLevel.Trace;
    public bool UseUtcTimestamp { get; set; } = false;
    public bool MultiLineFormat { get; set; } = false;
    public bool IndentMultilineMessages { get; set; } = true;
    public bool EnableConsoleColors { get; set; } = true;
    public Func<LogMessage, string>? LogEntryFormatter { get; set; }

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
        if (EnableConsoleColors)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.Write($"{message.TimeStamp}|");
            Console.ForegroundColor = LogLevelColors[message.LogLevel];
            Console.Write(LogMessage.LogLevelToString(message.LogLevel));
            Console.ForegroundColor = originalColor;
            Console.Write($"|{message.CategoryName}|");
            Console.ForegroundColor = LogLevelColors[message.LogLevel];

            if (IndentMultilineMessages)
            {
                Console.WriteLine(message.PaddedMessage);
            }
            else
            {
                Console.WriteLine(message.Message);
            }

            Console.ForegroundColor = originalColor;
        }
        else
        {
            if (IndentMultilineMessages)
            {
                Console.WriteLine($"{message.Header}{message.PaddedMessage}");
            }
            else
            {
                Console.WriteLine($"{message.Header}{message.Message}");
            }
        }
    }

    private void WriteMultiLineFormatMessage(LogMessage message)
    {
        if (EnableConsoleColors)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.Write($"[{message.TimeStamp}|");
            Console.ForegroundColor = LogLevelColors[message.LogLevel];
            Console.Write(LogMessage.LogLevelToString(message.LogLevel));
            Console.ForegroundColor = originalColor;
            Console.Write($"|{message.CategoryName}]{Environment.NewLine}");
            Console.ForegroundColor = LogLevelColors[message.LogLevel];
            Console.WriteLine($"{message.Message}{Environment.NewLine}");
            Console.ForegroundColor = originalColor;
        }
        else
        {
            Console.Write($"[{message.TimeStamp}|");
            Console.Write(LogMessage.LogLevelToString(message.LogLevel));
            Console.Write($"|{message.CategoryName}]{Environment.NewLine}");
            Console.WriteLine($"{message.Message}{Environment.NewLine}");
        }
    }

    internal void EnqueueMessage(LogMessage message)
    {
        if (_messageQueue.IsAddingCompleted == false)
        {
            try
            {
                _messageQueue.Add(message);
                return;
            }
            catch (InvalidOperationException) { }
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

        _loggers.Clear();
    }
}
