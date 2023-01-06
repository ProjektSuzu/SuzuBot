using Microsoft.Extensions.Logging;

namespace SuzuBot.Utils;
public static class LogUtils
{
    private static ILoggerFactory _loggerFactory { get; set; } = LoggerFactory.Create(builder =>
    {
        builder
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddConsole();
    });
    public static void SetLoggerProvider(ILoggerProvider loggerProvider)
    {
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddProvider(loggerProvider);
        });
    }
    public static void SetLoggerProvider<T>()
        where T : ILoggerProvider
    {
        var provider = (ILoggerProvider)Activator.CreateInstance(typeof(T));
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddProvider(provider);
        });
    }
    public static ILogger CreateLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }
    public static ILogger CreateLogger(string categoryName)
    {
        return _loggerFactory.CreateLogger(categoryName);
    }
}
