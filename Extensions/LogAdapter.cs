using Discord;
using Firefly.Options;
using Microsoft.Extensions.Options;

namespace Firefly.Extensions;

public class LogAdapter<T>(ILogger<T> logger, IOptions<DiscordOptions> options)
    where T : class
{
    private readonly Func<LogMessage, Exception?, string> _formatter = options.Value.LogFormat;

    public Task Log(LogMessage message)
    {
        logger.Log(GetLogLevel(message.Severity), default, message, message.Exception, _formatter);
        return Task.CompletedTask;
    }

    private static LogLevel GetLogLevel(LogSeverity severity)
    {
        return severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.None
        };
    }
}
