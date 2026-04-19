using Microsoft.Extensions.Logging;

namespace ConsoleLoggerLibrary;

internal sealed class ConsoleLogger : ILogger
{
    private readonly ConsoleLoggerProvider _consoleLoggerProvider;
    private readonly string _categoryName;

    public ConsoleLogger(ConsoleLoggerProvider consoleLoggerProvider, string categoryName)
    {
        ArgumentNullException.ThrowIfNull(consoleLoggerProvider);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);

        _consoleLoggerProvider = consoleLoggerProvider;
        _categoryName = categoryName;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && logLevel >= _consoleLoggerProvider.LogMinLevel;
    }

    IDisposable? ILogger.BeginScope<TState>(TState state)
    {
        return NullScope.Instance;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel) == false)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(formatter);
        string message = formatter(state, exception);
        LogMessage logMessage = new(
            message,
            exception,
            logLevel,
            _categoryName,
            eventId,
            _consoleLoggerProvider.UseUtcTimestamp);
        _consoleLoggerProvider.EnqueueMessage(logMessage);
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        private NullScope() { }
        public void Dispose() { }
    }
}
