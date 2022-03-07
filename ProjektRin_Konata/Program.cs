using Konata.Core.Message;
using NLog;
using ProjektRin.System;
using ProjektRin.Utils.BuildStamp;
using ProjektRin.Utils.Database;

namespace ProjektRin;

public static class Program
{
    private static string TAG = "Main";
    private static readonly Logger Logger = LogManager.GetLogger(TAG);
    public static int Main()
    {
        Console.WriteLine(
    @"==================================================" + "\n" +
    @"  _____  _____   ____       _ ______ _  _________ " + "\n" +
    @" |  __ \|  __ \ / __ \     | |  ____| |/ /__   __|" + "\n" +
    @" | |__) | |__) | |  | |    | | |__  | ' /   | |   " + "\n" +
    @" |  ___/|  _  /| |  | |_   | |  __| |  <    | |   " + "\n" +
    @" | |    | | \ \| |__| | |__| | |____| . \   | |   " + "\n" +
    @" |_|___ |_|__\_\\____/ \____/|______|_|\_\  |_|   " + "\n" +
    @" |  __ \|_   _| \ | |                             " + "\n" +
    @" | |__) | | | |  \| |                             " + "\n" +
    @" |  _  /  | | | . ` |                             " + "\n" +
    @" | | \ \ _| |_| |\  |                             " + "\n" +
    @" |_|  \_\_____|_| \_|                             " + "\n" +
    @"==================================================" + "\n" +
    $"ProjektRin {RinBuildStamp.Version}" + "\n" +
    $"Build: {RinBuildStamp.Branch}@{RinBuildStamp.CommitHash}" + "\n" +
    $"Time: {RinBuildStamp.BuildTime}" + "\n\n" +
    @"Powered by Konata (C)" + "\n" +
    $"Core: {CoreBuildStamp.Version} {CoreBuildStamp.Branch}@{CoreBuildStamp.CommitHash}" +
    "\n"
    );
        Logger.Info("\n\n\n\n");
        Logger.Info($"Current Dir: {AppDomain.CurrentDomain.BaseDirectory}");
        Logger.Info("Initializing Bot.");
        BotManager.Instance.InitBot();
        Logger.Info("Initializing Database.");
        if (!DatabaseManager.Instance.OpenConnection())
        {
            Logger.Fatal("Aborting.");
            return -1;
        }
        Logger.Info("Loading Commands.");
        CommandManager.Instance.LoadCommandSet();
        Logger.Info("Logging in.");

        var maxAttempt = 5;
        var attempt = 0;

        for (; attempt < maxAttempt; attempt++)
        {
            if (BotManager.Instance.LoginBot())
            {
                break;
            }
        }

        if (!BotManager.Instance.Bot.IsOnline())
        {
            Logger.Fatal("Bot Login Failed.");
            return -1;
        }


#if DEBUG
        uint devGroupUin = 644504300;
        _ = BotManager.Instance.Bot.SendGroupMessage(devGroupUin, new MessageBuilder("[ProjektRin]DEBUG" + "\n" +
            $"UTC {DateTime.UtcNow:s}" + "\n" +
            "RinBot启动成功" + "\n\n" +
            $"{RinBuildStamp.Version} {RinBuildStamp.Branch}@{RinBuildStamp.CommitHash}" + "\n" +
            $"构建时间: UTC {RinBuildStamp.BuildTime}"));
#else
        uint devGroupUin = 644504300;
        _ = BotManager.Instance.Bot.SendGroupMessage(devGroupUin, new MessageBuilder("[ProjektRin]" + "\n" +
            $"UTC {DateTime.UtcNow:s}" + "\n" +
            "Github Actions 自动构建任务成功" + "\n" +
            "RinBot启动成功" + "\n\n" +
            $"{RinBuildStamp.Version} {RinBuildStamp.Branch}@{RinBuildStamp.CommitHash}" + "\n" +
            $"构建时间: UTC {RinBuildStamp.BuildTime}"));
#endif

        Logger.Info("Bot started.");
        return 0;
    }

}