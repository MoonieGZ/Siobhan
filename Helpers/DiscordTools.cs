using Discord.WebSocket;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Firefly.Helpers;

public static class DiscordTools
{
    public static SocketTextChannel? DiscordLogChannel { get; set; }
    public static DiscordShardedClient? Client { get; set; }
}
