using System.Reflection;
using System.Text.Json;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Firefly.Extensions;
using Firefly.Helpers;
using Firefly.Options;
using Microsoft.Extensions.Options;
using Serilog.Context;
using IResult = Discord.Interactions.IResult;
using ExecuteResult = Discord.Interactions.ExecuteResult;

namespace Firefly.Services;

public class DiscordService(
    ILogger<DiscordService> logger,
    IOptions<DiscordOptions> discordBotOptions,
    DiscordShardedClient discordShardedClient,
    InteractionService interactionService,
    InteractiveService interactiveService,
    CommandService commandService,
    LogAdapter<BaseSocketClient> adapter,
    IServiceProvider serviceProvider) : BackgroundService
{
    private int _shardsReady;
    private TaskCompletionSource<bool>? _taskCompletionSource;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            discordShardedClient.Log += adapter.Log;
            discordShardedClient.ShardReady += OnShardReady;
            discordShardedClient.ShardDisconnected += OnShardDisconnected;

            commandService.Log += adapter.Log;

            discordShardedClient.MessageReceived += OnMessageReceived;
            discordShardedClient.MessageUpdated += OnMessageUpdated;
            discordShardedClient.MessageDeleted += OnMessageDeleted;

            discordShardedClient.UserJoined += OnUserJoined;
            discordShardedClient.UserLeft += OnUserLeft;

            discordShardedClient.InteractionCreated += OnInteractionCreated;
            interactionService.SlashCommandExecuted += OnSlashCommandExecuted;

            PrepareClientAwaiter();
            await discordShardedClient.LoginAsync(TokenType.Bot, discordBotOptions.Value.Token);
            await discordShardedClient.StartAsync();
            await WaitForReadyAsync(stoppingToken);

            await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
            await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);

            await discordShardedClient.Rest.DeleteAllGlobalCommandsAsync();
            await discordShardedClient.GetGuild(discordBotOptions.Value.ServerId).DeleteApplicationCommandsAsync();

            await interactionService.RegisterCommandsToGuildAsync(discordBotOptions.Value.ServerId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while starting the Discord service.");
        }
    }

    private static Task OnUserJoined(SocketGuildUser arg)
    {
        var embed = EmbedExtensions.MakeEmbed(new Color(253, 166, 224));
        embed.Title = "New Friend!";
        embed.Description = "──────────────── ⋆⋅☆⋅⋆ ────────────────\n" +
                            $"Welcome member #**{arg.Guild.MemberCount}** to **Event Horizon**.\n" +
                            $"Make sure to familiarize yourself with the <#{Keys.Channels.Rules}>!\n" +
                            "Pull up a seat and get cozy!\n" +
                            "──────────────── ⋆⋅☆⋅⋆ ────────────────";
        embed.ImageUrl = Keys.Images.Welcome;
        arg.Guild.GetTextChannel(Keys.Channels.Hangout)
            .SendMessageAsync($"Hi there {arg.Mention}!", embed: embed.Build());

        var adminEmbed = EmbedExtensions.MakeEmbed(new Color(253, 166, 224));
        adminEmbed.Description = $"User {arg.Mention} joined the server.";
        adminEmbed.AddField("Username", arg.Username, true);
        adminEmbed.AddField("User ID", arg.Id, true);
        adminEmbed.AddField("Creation Date", arg.CreatedAt.ToString("G"), true);

        DiscordTools.DiscordLogChannel!.SendMessageAsync(embed: adminEmbed.Build());

        return Task.CompletedTask;
    }

    private static Task OnUserLeft(SocketGuild arg1, SocketUser arg2)
    {
        var adminEmbed = EmbedExtensions.MakeEmbed(Color.Red);
        adminEmbed.Description = $"User {arg2.Mention} left the server.";
        adminEmbed.AddField("Username", arg2.Username, true);
        adminEmbed.AddField("User ID", arg2.Id, true);
        adminEmbed.AddField("Creation Date", arg2.CreatedAt.ToString("G"), true);

        DiscordTools.DiscordLogChannel!.SendMessageAsync(embed: adminEmbed.Build());

        return Task.CompletedTask;
    }

    private static Task OnMessageReceived(SocketMessage arg)
    {
        // ignored
        return Task.CompletedTask;
    }

    private static Task OnMessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2,
        ISocketMessageChannel arg3)
    {
        // ignored
        return Task.CompletedTask;
    }

    private static Task OnMessageDeleted(Cacheable<IMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2)
    {
        // ignored
        return Task.CompletedTask;
    }

    private async Task OnInteractionCreated(SocketInteraction socketInteraction)
    {
        if (socketInteraction is SocketMessageComponent messageComponent &&
            interactiveService.Callbacks.ContainsKey(messageComponent.Message.Id))
            return;

        await interactionService.ExecuteCommandAsync(
            new ShardedInteractionContext(discordShardedClient, socketInteraction), serviceProvider);
    }

    private async Task OnSlashCommandExecuted(SlashCommandInfo commandInfo, IInteractionContext interactionContext,
        IResult interactionResult)
    {
        if (interactionResult.IsSuccess || !interactionResult.Error.HasValue)
            return;

        if (interactionResult.Error == InteractionCommandError.UnmetPrecondition)
            return;

        if (!interactionContext.Interaction.HasResponded)
            await interactionContext.Interaction.DeferAsync(true);

        var errorEmbed = EmbedExtensions.MakeErrorEmbed();
        errorEmbed.Title = "Failed to execute command.";

        var debugOptions = new List<string>();
        var options = ((SocketSlashCommand)interactionContext.Interaction).Data;

        if (options != null && options.Options.Count > 0)
            debugOptions.AddRange(options.Options.Select(socketSlashCommandDataOption =>
                $"{socketSlashCommandDataOption.Name}: {socketSlashCommandDataOption.Value}"));

        var errorMessage = interactionResult is ExecuteResult eResult
            ? $"{eResult.Exception.GetType()}: {interactionResult.ErrorReason}"
            : $"{interactionResult.Error}: {interactionResult.ErrorReason}";

        errorEmbed.AddField("Author", interactionContext.User.Mention);
        errorEmbed.AddField("Channel", $"<#{interactionContext.Channel.Id}>");
        errorEmbed.AddField("Command", $"```{options!.Name}```");
        errorEmbed.AddField("Parameters", Format.Code(JsonSerializer.Serialize(debugOptions), "json"));
        errorEmbed.AddField("Error", $"```{errorMessage}```");

        using (LogContext.PushProperty("context", new
               {
                   Sender = interactionContext.User.ToString(),
                   CommandName = options.Name,
                   CommandParameters = JsonSerializer.Serialize(debugOptions),
                   ServerId = interactionContext.Interaction.GuildId ?? 0
               }))
        {
            if (interactionResult is ExecuteResult exResult)
                logger.LogError("{Error}: {ErrorReason}", exResult.Exception.GetType(), interactionResult.ErrorReason);
            else
                logger.LogError("{Error}: {ErrorReason}", interactionResult.Error, interactionResult.ErrorReason);
        }

        var builtEmbed = errorEmbed.Build();

        await interactionContext.Interaction.FollowupAsync(
            Localization.CmdFailed, ephemeral: true);
        await DiscordTools.DiscordLogChannel!.SendMessageAsync($"<@{Keys.Users.Owner}>", embed: builtEmbed);
    }

    private void PrepareClientAwaiter()
    {
        _taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _shardsReady = 0;

        discordShardedClient.ShardReady += OnShardReady;
    }

    private Task OnShardReady(DiscordSocketClient discordClient)
    {
        logger.LogInformation(
            "Connected as {name}#{discriminator}", discordClient.CurrentUser.Username,
            discordClient.CurrentUser.DiscriminatorValue);

        DiscordTools.DiscordLogChannel =
            (SocketTextChannel)discordClient.GetChannel(discordBotOptions.Value.LogChannelId);
        DiscordTools.Client = discordShardedClient;

        discordShardedClient.SetStatusAsync(UserStatus.Online);
        discordShardedClient.SetCustomStatusAsync("I will set the seas ablaze.");

        _shardsReady++;

        if (_shardsReady != discordShardedClient.Shards.Count)
            return Task.CompletedTask;

        _taskCompletionSource!.TrySetResult(true);
        discordShardedClient.ShardReady -= OnShardReady;

        return Task.CompletedTask;
    }

    private async Task OnShardDisconnected(Exception arg1, DiscordSocketClient arg2)
    {
        logger.LogError(arg1, "Disconnected from gateway.");

        if (arg1.InnerException is GatewayReconnectException &&
            arg1.InnerException.Message == "Server missed last heartbeat")
        {
            await arg2.StopAsync();
            await Task.Delay(10000);
            await arg2.StartAsync();
        }
    }

    private Task WaitForReadyAsync(CancellationToken cancellationToken)
    {
        if (_taskCompletionSource is null)
            throw new InvalidOperationException(
                "The sharded client has not been registered correctly. Did you use ConfigureDiscordShardedHost on your HostBuilder?");

        if (_taskCompletionSource.Task.IsCompleted)
            return _taskCompletionSource.Task;

        var registration = cancellationToken.Register(
            state => { ((TaskCompletionSource<bool>)state!).TrySetResult(true); },
            _taskCompletionSource);

        return _taskCompletionSource.Task.ContinueWith(_ => registration.DisposeAsync(), cancellationToken);
    }
}
