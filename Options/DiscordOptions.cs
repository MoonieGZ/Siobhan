using Discord;

// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Firefly.Options;

public class DiscordOptions
{
    public string? Token { get; set; }
    public ulong ServerId { get; set; }
    public ulong LogChannelId { get; set; }

    public Func<LogMessage, Exception?, string> LogFormat { get; set; } =
        (message, _) => $"{message.Source}: {message.Message}";
}
