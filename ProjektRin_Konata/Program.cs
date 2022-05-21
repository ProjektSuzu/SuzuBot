using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using NLog;
using ProjektRin.Components;
using ProjektRin.Utils.BuildStamp;
using ProjektRin.Utils.Database;

namespace ProjektRin;

public static class Program
{
    private static readonly string TAG = "Main";
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
    $"Konata.Core: {CoreBuildStamp.Version} {CoreBuildStamp.Branch}@{CoreBuildStamp.CommitHash}" + "\n" +
    @"Powered by Konata (C)" + "\n" +
    "\n"
    );
        Logger.Info("\n\n\n\n");
        Logger.Info("Initializing Bot.");
        BotManager.Instance.InitBot();

        Logger.Info("Logging in.");
        var result = BotManager.Instance.Bot.Login().Result;
        if (result)
        {
            Logger.Info("Bot online.");
        }
        else
        {
            Logger.Info("Bot login failed.");
        }

        return 0;
    }

}