# ConsoleLogger - Simple is Good
[![Nuget Release](https://img.shields.io/nuget/v/CodeFoxtrot.ConsoleLogger?style=for-the-badge)](https://www.nuget.org/packages/CodeFoxtrot.ConsoleLogger/)

* Cross-platform implementation supporting asynchronous Console logging.
* Configurable default minimum log level.
* Single-line, Multi-line or Custom log entry formats.
* Indent multiline messages for easier reading and analysis.
* Configurable color scheme for Console log messages, for easier reading.

![Snag_16287bf1](https://user-images.githubusercontent.com/41308769/177916624-85be1c05-490d-4c6b-90c8-fb77ed04950d.png)

### Single-line Format
![Snag_1628ce28](https://user-images.githubusercontent.com/41308769/177916665-9a8e8447-9833-4aa2-a0d3-9fca1cd46eb0.png)

### Multi-line Format
![Snag_162914c6](https://user-images.githubusercontent.com/41308769/177916695-50500daf-be95-48d6-82ad-b3e183c5f215.png)

## How to use

### Scenario #1: Quickstart
```
using ConsoleLoggerLibrary;

...<omitted>...

.ConfigureLogging((context, builder) =>
  {
    builder.ClearProviders();
    builder.AddConsoleLogger();
  })
```

### Scenario #2: Using appsettings.json
  
**appsettings.json** -- all options shown
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Error"
    },
    "ConsoleLogger": {
      "LogMinLevel": "Debug",
      "MultilineFormat": false,
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
```
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
                    builder.AddConsoleLogger(context.Configuration);
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
  
### Scenario #3: Using ConfigureLogging
  
**Program.cs**  -- full file for complete context, all ConsoleLoggerOptions shown
```
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
                        configure.MultiLineFormat = false;
                        configure.IndentMultilineMessages = true;
                        configure.EnableConsoleColors = true;
                        configure.LogLevelColors = new Dictionary<LogLevel, ConsoleColor>()
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

## Indentation 
IndentMultilineMessages=**true**
```
2022-04-04--18.10.20|INFO|ConsoleLoggerDemo.App|{
                                                 "Date": "6/24/2022",
                                                 "Location": "Center Moriches",
                                                 "TemperatureCelsius": 20,
                                                 "Summary": "Nice"
                                                }
```
  
IndentMultilineMessages=**false**
```
2022-04-04--18.11.19|INFO|ConsoleLoggerDemo.App|{
  "Date": "6/24/2022",
  "Location": "Center Moriches",
  "TemperatureCelsius": 20,
  "Summary": "Nice"
}
```
Note: The IndentMultilineMessages option is only for the Single-Line message format.

## Roadmap
* Support for UTC timestamps, instead of local time.

## Reference
https://docs.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider
