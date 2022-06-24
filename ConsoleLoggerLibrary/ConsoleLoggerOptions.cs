using Microsoft.Extensions.Logging;

namespace ConsoleLoggerLibrary;

public class ConsoleLoggerOptions
{
    public LogLevel LogMinLevel { get; set; } = LogLevel.Trace;

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
}
