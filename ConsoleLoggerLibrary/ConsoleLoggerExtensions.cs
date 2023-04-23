using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConsoleLoggerLibrary;

public static class ConsoleLoggerExtensions
{
    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, ConsoleLoggerProvider>(
            sp => new ConsoleLoggerProvider(LogLevel.Information));
        builder.SetMinimumLevel(LogLevel.Information);
        return builder;
    }

    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder, LogLevel minLevel)
    {
        builder.Services.AddSingleton<ILoggerProvider, ConsoleLoggerProvider>(
            sp => new ConsoleLoggerProvider(minLevel));
        builder.SetMinimumLevel(minLevel);
        return builder;
    }

    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder, LogLevel minLevel, bool useUtcTimestamp)
    {
        builder.Services.AddSingleton<ILoggerProvider, ConsoleLoggerProvider>(
            sp => new ConsoleLoggerProvider(minLevel, useUtcTimestamp));
        builder.SetMinimumLevel(minLevel);
        return builder;
    }

    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder, LogLevel minLevel, bool useUtcTimestamp, bool multiLineFormat)
    {
        builder.Services.AddSingleton<ILoggerProvider, ConsoleLoggerProvider>(
            sp => new ConsoleLoggerProvider(minLevel, useUtcTimestamp, multiLineFormat));
        builder.SetMinimumLevel(minLevel);
        return builder;
    }

    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder, LogLevel minLevel, bool useUtcTimestamp, bool multiLineFormat, bool indentMultilineMessages)
    {
        builder.Services.AddSingleton<ILoggerProvider, ConsoleLoggerProvider>(
            sp => new ConsoleLoggerProvider(minLevel, useUtcTimestamp, multiLineFormat, indentMultilineMessages));
        builder.SetMinimumLevel(minLevel);
        return builder;
    }

    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder, LogLevel minLevel, bool useUtcTimestamp, bool multiLineFormat, bool indentMultilineMessages, bool enableConsoleColors)
    {
        builder.Services.AddSingleton<ILoggerProvider, ConsoleLoggerProvider>(
            sp => new ConsoleLoggerProvider(minLevel, useUtcTimestamp, multiLineFormat, indentMultilineMessages, enableConsoleColors));
        builder.SetMinimumLevel(minLevel);
        return builder;
    }

    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder, Action<ConsoleLoggerOptions> configure)
    {
        ConsoleLoggerOptions options = new();
        configure(options);
        builder.Services.AddSingleton<ILoggerProvider, ConsoleLoggerProvider>(
            sp => new ConsoleLoggerProvider(options));
        builder.SetMinimumLevel(options.LogMinLevel);
        return builder;
    }

    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder, IConfiguration configuration, Action<ConsoleLoggerOptions> configure = null)
    {
        ConsoleLoggerProvider consoleLoggerProvider = CreateFromConfiguration(configuration, configure = null);

        if (consoleLoggerProvider != null)
        {
            builder.Services.AddSingleton<ILoggerProvider, ConsoleLoggerProvider>(
                sp => consoleLoggerProvider);
        }

        builder.SetMinimumLevel(consoleLoggerProvider.LogMinLevel);
        return builder;
    }

    private static ConsoleLoggerProvider CreateFromConfiguration(IConfiguration configuration, Action<ConsoleLoggerOptions> configure)
    {
        IConfigurationSection consoleLogger = configuration.GetSection("Logging:ConsoleLogger");

        if (consoleLogger == null)
        {
            return null;
        }

        ConsoleLoggerOptions options = new();
        string minLevel = consoleLogger["LogMinLevel"];

        if (string.IsNullOrWhiteSpace(minLevel) == false 
            && Enum.TryParse(minLevel, out LogLevel level))
        {
            options.LogMinLevel = level;
        }

        string useUtcTimestamp = consoleLogger["UseUtcTimestamp"];

        if (string.IsNullOrWhiteSpace(useUtcTimestamp) == false
            && bool.TryParse(useUtcTimestamp, out bool useUtcTime))
        {
            options.UseUtcTimestamp = useUtcTime;
        }

        string multiLineFormat = consoleLogger["MultilineFormat"];

        if (string.IsNullOrWhiteSpace(multiLineFormat) == false 
            && bool.TryParse(multiLineFormat, out bool multiLine))
        {
            options.MultiLineFormat = multiLine;
        }

        string indentMultilineMessages = consoleLogger["IndentMultilineMessages"];

        if (string.IsNullOrWhiteSpace(indentMultilineMessages) == false 
            && bool.TryParse(indentMultilineMessages, out bool indent))
        {
            options.IndentMultilineMessages = indent;
        }

        string enableConsoleColors = consoleLogger["EnableConsoleColors"];

        if (string.IsNullOrWhiteSpace(enableConsoleColors) == false 
            && bool.TryParse(enableConsoleColors, out bool colors))
        {
            options.EnableConsoleColors = colors;
        }

        Dictionary<LogLevel, ConsoleColor> logLevelColors =
            consoleLogger.GetSection("LogLevelColors").GetChildren().ToDictionary(x =>
            {
                Enum.TryParse(x.Key, out LogLevel level);
                return level;
            },
            x =>
            {
                Enum.TryParse(x.Value, out ConsoleColor color);
                return color;
            });

        if (logLevelColors != null)
        {
            options.LogLevelColors = logLevelColors;
        }

        // Override IConfiguration with any provided code-based configuration
        configure?.Invoke(options);

        return new ConsoleLoggerProvider(options);
    }
}
