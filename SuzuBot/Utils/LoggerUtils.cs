using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace SuzuBot.Utils;
public static class LoggerUtils
{
    public static ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
    {
        builder
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddNLog();
    });
}
