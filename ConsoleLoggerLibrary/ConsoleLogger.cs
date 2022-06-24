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

    public void Log(string message, LogLevel logLevel)
    {
        LogMessage msg = new(logLevel, _categoryName, message);
        _consoleLoggerProvider.Log(msg);
    }

    public void Log(Exception e)
    {
        LogMessage msg = new(LogLevel.Error, _categoryName, e.Message);
        _consoleLoggerProvider.Log(msg);
    }

    public void LogCritical(string message)
    {
        LogMessage msg = new(LogLevel.Critical, _categoryName, message);
        _consoleLoggerProvider.Log(msg);
    }

    public void LogDebug(string message)
    {
        LogMessage msg = new(LogLevel.Debug, _categoryName, message);
        _consoleLoggerProvider.Log(msg);
    }

    public void LogError(string message)
    {
        LogMessage msg = new(LogLevel.Error, _categoryName, message);
        _consoleLoggerProvider.Log(msg);
    }

    public void LogInformation(string message)
    {
        LogMessage msg = new(LogLevel.Information, _categoryName, message);
        _consoleLoggerProvider.Log(msg);
    }

    public void LogTrace(string message)
    {
        LogMessage msg = new(LogLevel.Trace, _categoryName, message);
        _consoleLoggerProvider.Log(msg);
    }

    public void LogWarning(string message)
    {
        LogMessage msg = new(LogLevel.Warning, _categoryName, message);
        _consoleLoggerProvider.Log(msg);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (IsEnabled(logLevel) == false)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(nameof(formatter));

        switch (logLevel)
        {
            case LogLevel.Trace:
                LogTrace(formatter(state, exception));
                break;
            case LogLevel.Debug:
                LogDebug(formatter(state, exception));
                break;
            case LogLevel.Warning:
                LogWarning(formatter(state, exception));
                break;
            case LogLevel.Error:
                LogError(formatter(state, exception));
                break;
            case LogLevel.Critical:
                LogCritical(formatter(state, exception));
                break;
            case LogLevel.None:
                Log(formatter(state, exception), LogLevel.None);
                break;
            case LogLevel.Information:
            default:
                LogInformation(formatter(state, exception));
                break;
        }
    }
}
