using ConsoleLoggerLibrary;
using Microsoft.Extensions.Logging;

namespace ConsleLoggerLibrary;

internal class ConsoleLogger : ILogger
{
    private readonly ConsoleLoggerProvider _consoleLoggerProvider;
    private readonly string _categoryName;

    public ConsoleLogger(ConsoleLoggerProvider consoleLoggerProvider, string categoryName)
    {
        _consoleLoggerProvider = consoleLoggerProvider ?? throw new ArgumentException("Log provider must not be NULL");

        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new ArgumentException("Log name must not be NULL or empty");
        }

        _categoryName = categoryName;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _consoleLoggerProvider.LogMinLevel;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (IsEnabled(logLevel) == false)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(nameof(formatter));
        string message = formatter(state, exception);
        _consoleLoggerProvider.EnqueueMessage(new LogMessage(message, exception, logLevel, _categoryName, eventId));
    }
}
