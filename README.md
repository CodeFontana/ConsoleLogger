# ConsoleLogger - Simple is Good
[![Nuget Release](https://img.shields.io/nuget/v/CodeFoxtrot.ConsoleLogger?style=for-the-badge)](https://www.nuget.org/packages/CodeFoxtrot.ConsoleLogger/)

* Cross-platform implementation supporting asynchronous Console logging.
* Configurable default minimum log level.
* Indent multiline messages for easier reading and analysis.
* Configurable color scheme for Console log messages, for easier reading.

![image](https://user-images.githubusercontent.com/41308769/175711925-3e02c240-e211-46e1-9c6f-1eb16a68300d.png)
![image](https://user-images.githubusercontent.com/41308769/175713316-b1025222-2465-4eb7-abee-8333098a0683.png)


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
            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            bool isDevelopment = string.IsNullOrEmpty(env) || env.ToLower() == "development";

            await Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appSettings.json", true, true);
                    config.AddJsonFile($"appSettings.{env}.json", true, true);
                    config.AddUserSecrets<Program>(optional: true);
                    config.AddEnvironmentVariables();
                })
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
            string env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            bool isDevelopment = string.IsNullOrEmpty(env) || env.ToLower() == "development";

            await Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", true, true);
                    config.AddJsonFile($"appsettings.{env}.json", true, true);
                    config.AddUserSecrets<Program>(optional: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.ClearProviders();
                    builder.AddConsoleLogger(configure =>
                    {
                        configure.LogMinLevel = LogLevel.Trace;
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

## Roadmap
* Support custom formatter for log message.

## Reference
https://docs.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider
