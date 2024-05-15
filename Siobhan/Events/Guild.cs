using DisCatSharp;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using Siobhan.Services;

namespace Siobhan.Events;

[EventHandler]
public class Guild
{
    public ConfigService config { private get; set; }

    [Event]
    async Task GuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs args)
    {
        client.Logger.LogInformation($"{args.Member.Username} joined {args.Guild.Name}");
        Console.WriteLine(config);
        config.a();
    }
}