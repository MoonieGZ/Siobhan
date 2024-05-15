using Discord;
using Discord.WebSocket;
using Fergun.Interactive;
using Firefly.Extensions;
using Firefly.Helpers;
using Serilog;
using Serilog.Events;

namespace Firefly;

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
            .WriteTo.File("Logs/latest-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.Host.UseSerilog();

            builder.Services
                .AddDiscord(
                    discordClient =>
                    {
                        discordClient.GatewayIntents = (GatewayIntents.AllUnprivileged
                                                        & ~GatewayIntents.GuildInvites
                                                        & ~GatewayIntents.GuildScheduledEvents)
                                                       | GatewayIntents.GuildMembers
                                                       | GatewayIntents.MessageContent;
                        discordClient.AlwaysDownloadUsers = true;
                        discordClient.MessageCacheSize = 1000;
                    },
                    _ => { },
                    textCommandsService => { textCommandsService.CaseSensitiveCommands = false; },
                    builder.Configuration)
                .AddLogging(options => options.AddSerilog(dispose: true))
                .AddSingleton<LogAdapter<BaseSocketClient>>()
                .AddSingleton<InteractiveService>();

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
