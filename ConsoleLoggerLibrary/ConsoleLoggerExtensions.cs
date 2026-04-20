using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace ConsoleLoggerLibrary;

public static class ConsoleLoggerExtensions
{
    /// <summary>
    /// Registers the console logger provider. When the host has registered
    /// an <see cref="IConfiguration"/> (e.g. via <c>Host.CreateDefaultBuilder</c>),
    /// options are automatically bound from the <c>Logging:ConsoleLogger</c>
    /// section, and the framework's <c>Logging:ConsoleLogger:LogLevel</c>
    /// subsection enables per-provider log level filtering.
    /// </summary>
    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, ConsoleLoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions<ConsoleLoggerOptions, ConsoleLoggerProvider>(builder.Services);

        // Lower the framework-wide MinLevel floor to match the configured
        // ConsoleLoggerOptions.LogMinLevel when it is more permissive than
        // the framework default (Information). Without this, an option such
        // as LogMinLevel = Trace would still be blocked by the framework's
        // hard floor before reaching this provider.
        builder.Services
            .AddOptions<LoggerFilterOptions>()
            .Configure<IOptionsMonitor<ConsoleLoggerOptions>>((filterOptions, monitor) =>
            {
                LogLevel optionsMinLevel = monitor.CurrentValue.LogMinLevel;
                if (optionsMinLevel < filterOptions.MinLevel)
                {
                    filterOptions.MinLevel = optionsMinLevel;
                }
            });

        return builder;
    }

    /// <summary>
    /// Registers the console logger provider and applies a code-based options
    /// configuration callback on top of any configuration-based bindings.
    /// </summary>
    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder, Action<ConsoleLoggerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.AddConsoleLogger();
        builder.Services.Configure(configure);

        return builder;
    }

    /// <summary>
    /// Registers the console logger provider and explicitly binds options
    /// from the supplied <paramref name="configuration"/>'s
    /// <c>Logging:ConsoleLogger</c> section. An optional
    /// <paramref name="configure"/> callback runs after binding so code can
    /// override individual values.
    /// </summary>
    public static ILoggingBuilder AddConsoleLogger(this ILoggingBuilder builder, IConfiguration configuration, Action<ConsoleLoggerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.AddConsoleLogger();
        builder.Services.Configure<ConsoleLoggerOptions>(
            configuration.GetSection("Logging:ConsoleLogger"));

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        return builder;
    }
}
