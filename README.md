# ConsoleLogger - Simple is Good
[![Nuget Release](https://img.shields.io/nuget/v/CodeFoxtrot.ConsoleLogger?style=for-the-badge)](https://www.nuget.org/packages/CodeFoxtrot.ConsoleLogger/)

* Cross-platform implementation supporting asynchronous Console logging.
* Configurable default minimum log level.
* Single-line, Multi-line or Custom log entry formats.
* Indent multiline messages for easier reading and analysis.
* Configurable color scheme for Console log messages, for easier reading.
* Per-provider log level filtering via `Logging:ConsoleLogger:LogLevel` in `appsettings.json`.

## Target frameworks
.NET 8, .NET 9, .NET 10.

![Snag_16287bf1](https://user-images.githubusercontent.com/41308769/177916624-85be1c05-490d-4c6b-90c8-fb77ed04950d.png)

### Single-line Format
![Snag_1628ce28](https://user-images.githubusercontent.com/41308769/177916665-9a8e8447-9833-4aa2-a0d3-9fca1cd46eb0.png)

### Multi-line Format
![Snag_162914c6](https://user-images.githubusercontent.com/41308769/177916695-50500daf-be95-48d6-82ad-b3e183c5f215.png)

## How to use

### Scenario #1: Quickstart
```csharp
using ConsoleLoggerLibrary;

...<omitted>...

.ConfigureLogging((context, builder) =>
  {
    builder.ClearProviders();
    builder.AddConsoleLogger();
  })
```

### Scenario #2: Using appsettings.json

When the host registers `IConfiguration` (e.g. via `Host.CreateDefaultBuilder`),
the no-arg `AddConsoleLogger()` automatically binds options from the
`Logging:ConsoleLogger` section — no need to pass `IConfiguration` explicitly.

**appsettings.json** -- all options shown
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Error"
    },
    "ConsoleLogger": {
      "LogMinLevel": "Debug",
      "UseUtcTimestamp": false,
      "MultiLineFormat": false,
      "IndentMultilineMessages": true,
      "EnableConsoleColors": true,
      "LogLevelColors": {
        "Trace": "Cyan",
        "Debug": "Blue",
        "Information": "Green",
        "Warning": "Yellow",
        "Error": "Red",
        "Critical": "Magenta",
        "None": "White"
      }
    }
  }
}
```

**Program.cs** -- full file for complete context
```csharp
using ConsoleLoggerLibrary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleLoggerDemo;

internal class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureLogging((context, builder) =>
                {
                    builder.ClearProviders();
                    builder.AddConsoleLogger();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<App>();
                })
                .RunConsoleAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}
```

> If your host doesn't register `IConfiguration` automatically, pass it
> explicitly: `builder.AddConsoleLogger(context.Configuration);`

### Scenario #3: Using ConfigureLogging

**Program.cs**  -- full file for complete context, all ConsoleLoggerOptions shown
```csharp
using ConsoleLoggerLibrary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleLoggerDemo;

internal class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureLogging((context, builder) =>
                {
                    builder.ClearProviders();
                    builder.AddConsoleLogger(configure =>
                    {
                        configure.LogMinLevel = LogLevel.Trace;
                        configure.UseUtcTimestamp = false;
                        configure.MultiLineFormat = false;
                        configure.IndentMultilineMessages = true;
                        configure.EnableConsoleColors = true;
                        configure.LogLevelColors = new()
                        {
                            [LogLevel.Trace] = ConsoleColor.Cyan,
                            [LogLevel.Debug] = ConsoleColor.Blue,
                            [LogLevel.Information] = ConsoleColor.Green,
                            [LogLevel.Warning] = ConsoleColor.Yellow,
                            [LogLevel.Error] = ConsoleColor.Red,
                            [LogLevel.Critical] = ConsoleColor.DarkRed,
                            [LogLevel.None] = ConsoleColor.White
                        };
                    });
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<App>();
                })
                .RunConsoleAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}
```

## Per-provider log level filtering

Because the provider registers itself via
`LoggerProviderOptions.RegisterProviderOptions`, you can scope per-category
log levels to *just this provider* (independent of the global `Logging:LogLevel`
section) by adding a `LogLevel` subsection under `ConsoleLogger`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information"
  },
  "ConsoleLogger": {
    "LogMinLevel": "Trace",
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Warning",
      "MyApp.Diagnostics": "Trace"
    }
  }
}
```

`LogMinLevel` sets the floor honored by this provider. The framework's
global `MinLevel` is automatically lowered to match when `LogMinLevel`
is more permissive (e.g. `Trace`), so per-provider Trace/Debug entries
aren't filtered out before reaching the writer.

## Indentation
IndentMultilineMessages=**true**
```text
2026-04-19--18.10.20|INFO|ConsoleLoggerDemo.App|{
                                                 "Date": "4/19/2026",
                                                 "Location": "Center Moriches",
                                                 "TemperatureCelsius": 20,
                                                 "Summary": "Nice"
                                                }
```

IndentMultilineMessages=**false**
```text
2026-04-19--18.11.19|INFO|ConsoleLoggerDemo.App|{
  "Date": "4/19/2026",
  "Location": "Center Moriches",
  "TemperatureCelsius": 20,
  "Summary": "Nice"
}
```
Note: The IndentMultilineMessages option is only for the Single-Line message format.

## Debugging
The package ships portable PDBs with
[Source Link](https://github.com/dotnet/sourcelink) embedded and a
companion `.snupkg` symbol package. Enable "Source Link support" in
Visual Studio (or JetBrains Rider) to step into the library directly
from your debugger.

## Releases
See [GitHub Releases](https://github.com/CodeFontana/ConsoleLogger/releases)
for the changelog.

## Reference
https://docs.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider
