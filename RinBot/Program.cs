using Microsoft.Extensions.Logging;
using RinBot.Core;
using RinBot.Utils;

namespace RinBot;
public static class Program
{
    private static ILogger _logger = LoggerUtils.LoggerFactory.CreateLogger("Boot");

    public static async Task Main()
    {
        string title =
            """
            ______ _      ______       _   
            | ___ (_)     | ___ \     | |  
            | |_/ /_ _ __ | |_/ / ___ | |_ 
            |    /| | '_ \| ___ \/ _ \| __|
            | |\ \| | | | | |_/ / (_) | |_ 
            \_| \_|_|_| |_\____/ \___/ \__|
            ===============================
            RinBot   RinBotDev 2020 GPL-3.0
            """;

        Console.WriteLine(title);
        _logger.LogInformation($"Version: RinBot_{RinBotBuildStamp.Version}");
        _logger.LogInformation($"Branch: {RinBotBuildStamp.Branch}@{RinBotBuildStamp.CommitHash[..8]}");

        Context context = new();
        await context.StartAsync();
    }
}