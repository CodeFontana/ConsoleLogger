using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConsoleLoggerLibrary;

public static class ConsoleLoggerExtensions
{
    /// <summary>
    /// Registers the console logger with default options. The default minimum
    /// level is <see cref="LogLevel.Information"/>; override per-category
    /// behavior via standard <c>Logging:LogLevel</c> configuration.
    /// </summary>
    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return AddConsoleLogger(builder, _ => { });
    }

    /// <summary>
    /// Registers the console logger using a code-based options configuration
    /// callback.
    /// </summary>
    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder, Action<ConsoleLoggerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        ConsoleLoggerOptions options = new();
        configure(options);

        builder.Services.AddSingleton<ILoggerProvider, ConsoleLoggerProvider>(
            sp => new ConsoleLoggerProvider(options));
        builder.SetMinimumLevel(options.LogMinLevel);
        return builder;
    }

    /// <summary>
    /// Registers the console logger using values from the
    /// <c>Logging:ConsoleLogger</c> configuration section. An optional
    /// <paramref name="configure"/> callback runs after binding so code can
    /// override individual values.
    /// </summary>
    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder, IConfiguration configuration, Action<ConsoleLoggerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        ConsoleLoggerProvider consoleLoggerProvider = CreateFromConfiguration(configuration, configure);

        builder.Services.AddSingleton<ILoggerProvider, ConsoleLoggerProvider>(
            sp => consoleLoggerProvider);
        builder.SetMinimumLevel(consoleLoggerProvider.LogMinLevel);

        return builder;
    }

    private static ConsoleLoggerProvider CreateFromConfiguration(IConfiguration configuration, Action<ConsoleLoggerOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection consoleLogger = configuration.GetSection("Logging:ConsoleLogger");
        ConsoleLoggerOptions options = new();
        string? minLevel = consoleLogger["LogMinLevel"];

        if (string.IsNullOrWhiteSpace(minLevel) == false
            && Enum.TryParse(minLevel, out LogLevel level))
        {
            options.LogMinLevel = level;
        }

        string? useUtcTimestamp = consoleLogger["UseUtcTimestamp"];

        if (string.IsNullOrWhiteSpace(useUtcTimestamp) == false
            && bool.TryParse(useUtcTimestamp, out bool useUtcTime))
        {
            options.UseUtcTimestamp = useUtcTime;
        }

        string? multiLineFormat = consoleLogger["MultilineFormat"];

        if (string.IsNullOrWhiteSpace(multiLineFormat) == false
            && bool.TryParse(multiLineFormat, out bool multiLine))
        {
            options.MultiLineFormat = multiLine;
        }

        string? indentMultilineMessages = consoleLogger["IndentMultilineMessages"];

        if (string.IsNullOrWhiteSpace(indentMultilineMessages) == false
            && bool.TryParse(indentMultilineMessages, out bool indent))
        {
            options.IndentMultilineMessages = indent;
        }

        string? enableConsoleColors = consoleLogger["EnableConsoleColors"];

        if (string.IsNullOrWhiteSpace(enableConsoleColors) == false
            && bool.TryParse(enableConsoleColors, out bool colors))
        {
            options.EnableConsoleColors = colors;
        }

        Dictionary<LogLevel, ConsoleColor> logLevelColors =
            consoleLogger.GetSection("LogLevelColors").GetChildren().ToDictionary(x =>
            {
                if (Enum.TryParse(x.Key, out LogLevel level) == false)
                {
                    throw new ArgumentException($"Invalid LogLevel value: {x.Key}");
                }

                return level;
            },
            x =>
            {
                if (Enum.TryParse(x.Value, out ConsoleColor color) == false)
                {
                    throw new ArgumentException($"Invalid ConsoleColor value: {x.Value}");
                }

                return color;
            });

        // Only overwrite the defaults when the configuration actually supplied
        // at least one color; otherwise an empty/missing section would wipe
        // out the defaults and cause KeyNotFoundException at write time.
        if (logLevelColors.Count > 0)
        {
            options.LogLevelColors = logLevelColors;
        }

        // Override IConfiguration with any provided code-based configuration
        configure?.Invoke(options);

        return new ConsoleLoggerProvider(options);
    }
}
