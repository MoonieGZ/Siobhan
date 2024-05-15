using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Firefly.Options;
using Firefly.Services;

namespace Firefly.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscord(
        this IServiceCollection serviceCollection,
        Action<DiscordSocketConfig> configureClient,
        Action<InteractionServiceConfig> configureInteractionService,
        Action<CommandServiceConfig> configureTextCommands,
        IConfiguration configuration)
    {
        var discordSocketConfig = new DiscordSocketConfig();
        configureClient(discordSocketConfig);
        var discordClient = new DiscordShardedClient(discordSocketConfig);

        var interactionServiceConfig = new InteractionServiceConfig();
        configureInteractionService(interactionServiceConfig);
        var interactionService = new InteractionService(discordClient, interactionServiceConfig);

        var commandServiceConfig = new CommandServiceConfig();
        configureTextCommands(commandServiceConfig);
        var textCommandService = new CommandService(commandServiceConfig);

        return serviceCollection
            .Configure<DiscordOptions>(configuration.GetSection("Discord"))
            .AddHostedService<DiscordService>()
            .AddSingleton(discordClient)
            .AddSingleton(interactionService)
            .AddSingleton(new InteractiveConfig { DefaultTimeout = TimeSpan.FromMinutes(5) })
            .AddSingleton<InteractiveService>()
            .AddSingleton(textCommandService);
    }
}
