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
