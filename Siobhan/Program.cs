using System.Reflection;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Enums;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Serilog.Events;
using Siobhan.Helpers;
using Siobhan.Services;

namespace Siobhan;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Starting bot...");
        FileHelpers.EnsureDirectoryExists("Logs");
        FileHelpers.EnsureDirectoryExists("Data");

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("Logs/latest-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14,
                restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        DiscordConfiguration discordConfiguration = new()
        {
            Token = args[0],
            LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger),
            AutoReconnect = true,
            FeedbackEmail = "alyxia@riseup.net",
            AttachUserInfo = true,
            Intents = DiscordIntents.All,
            ServiceProvider = new ServiceCollection()
                .AddSingleton<ConfigService>()
                .BuildServiceProvider()
        };
        DiscordClient discordClient = new(discordConfiguration);

        var appCommandModule = typeof(ApplicationCommandsModule);
        var commands = Assembly.GetExecutingAssembly().GetTypes().Where(t => appCommandModule.IsAssignableFrom(t) && !t.IsNested).ToList();

        var appCommandExt = discordClient.UseApplicationCommands();

        foreach (var command in commands)
			appCommandExt.RegisterGuildCommands(command, 843466538408869918);

        // Register event handlers across the entire project
        discordClient.RegisterEventHandlers(Assembly.GetExecutingAssembly());

        Console.WriteLine("Connecting to Discord...");
        await discordClient.ConnectAsync();

        discordClient.Logger.LogInformation(
            "Connection success! Logged in as {CurrentUserUsername}#{CurrentUserDiscriminator} ({CurrentUserId})",
            discordClient.CurrentUser.Username, discordClient.CurrentUser.Discriminator,
            discordClient.CurrentUser.Id);

        await Task.Delay(-1);
    }
}