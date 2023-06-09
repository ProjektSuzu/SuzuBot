using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using SuzuBot.Core;
using SuzuBot.Utils;

namespace SuzuBot;

public class Program
{
    public static async Task Main()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Environment.SetEnvironmentVariable("DOTNET_GCHeapHardLimit", "0x1E848000");
        }

        LogUtils.SetLoggerProvider<NLogLoggerProvider>();
        ILogger _logger = LogUtils.CreateLogger("Boot");
        string title =
            """
              _____                 ____        _   
             / ____|               |  _ \      | |  
            | (___  _   _ _____   _| |_) | ___ | |_ 
             \___ \| | | |_  / | | |  _ < / _ \| __|
             ____) | |_| |/ /| |_| | |_) | (_) | |_ 
            |_____/ \__,_/___|\__,_|____/ \___/ \__|
            ========================================
            SuzuBot     AkulaKirov 2020      GPL-3.0
            """;

        Console.WriteLine(title);
        _logger.LogInformation($"Version: SuzuBot_{SuzuBotBuildStamp.Version}");
        _logger.LogInformation($"Branch: {SuzuBotBuildStamp.Branch}@{SuzuBotBuildStamp.CommitHash[..8]}");
        Context ctx = new();
        if (!await ctx.StartAsync())
            Environment.Exit(-1);
    }
}