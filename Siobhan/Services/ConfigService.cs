using Siobhan.Helpers;

namespace Siobhan.Services;

public interface IConfig
{
    public ulong WelcomeChannel { get; set; }
    public ulong LogChannel { get; set; }
}

public class ConfigService
{
    public IConfig Config;

    public ConfigService()
    {
        Config = FileHelpers.ReadJson<IConfig>("config.json");
    }

    public void a()
    {
        Console.WriteLine("a called");
        Console.WriteLine(Config.WelcomeChannel);
    }
}