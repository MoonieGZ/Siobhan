using Serilog;
using Serilog.Events;
using Siobhan.Helpers;

namespace Siobhan;

public static class Program
{
    public static void Main()
    {
        FileHelpers.EnsureDirectoryExists("Logs");
        FileHelpers.EnsureDirectoryExists("Data");

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
#if DEBUG
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
#endif
            .WriteTo.Console()
            .WriteTo.File("Logs/latest-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14,
                restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.Host.UseSerilog();

            var app = builder.Build();
            app.UseSerilogRequestLogging();

            app.MapGet("/", () => "Hello World!");
            app.MapGet("/health", () => Results.Ok());

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}