using Microsoft.Extensions.Logging;

namespace ConsoleLoggerLibrary;

public sealed class ConsoleLoggerOptions
{
    public LogLevel LogMinLevel { get; set; } = LogLevel.Trace;

    public bool UseUtcTimestamp { get; set; } = false;

    public bool MultiLineFormat { get; set; } = false;

    public bool IndentMultilineMessages { get; set; } = true;

    public bool EnableConsoleColors { get; set; } = true;

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

    public Func<LogMessage, string> LogEntryFormatter { get; set; }
}
