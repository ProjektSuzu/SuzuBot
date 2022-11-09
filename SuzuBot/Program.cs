using Microsoft.Extensions.Logging;
using SuzuBot.Core;
using SuzuBot.Utils;

namespace SuzuBot;
public static class Program
{
    private static ILogger _logger = LoggerUtils.LoggerFactory.CreateLogger("Boot");

    public static async Task Main()
    {
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

        Context context = new();
        await context.StartAsync();
    }
}