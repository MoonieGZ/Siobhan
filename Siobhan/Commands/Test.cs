using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using Siobhan.Services;

namespace Siobhan.Commands;

public class Test : ApplicationCommandsModule
{
    [SlashCommand("test", "hello.")]
    public async Task MySlashCommand(InteractionContext ctx)
    {
        var config = ctx.Services.GetRequiredService<ConfigService>();
        ctx.Client.Logger.LogInformation($"{ctx.Member.Username} joined {ctx.Guild.Name}");
        Console.WriteLine(config);
        // config.a();
    }
}