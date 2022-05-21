using Konata.Core.Interfaces.Api;
using NLog;
using ProjektRin.Core.Components;
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
    @"=================================" + "\n" +
    @"    ____  _       ____        __ " + "\n" +
    @"   / __ \(_)___  / __ )____  / /_" + "\n" +
    @"  / /_/ / / __ \/ __  / __ \/ __/" + "\n" +
    @" / _, _/ / / / / /_/ / /_/ / /_  " + "\n" +
    @"/_/ |_/_/_/ /_/_____/\____/\__/  " + "\n" +
    @"=================================" + "\n" +
    $"RinBot {RinBuildStamp.Version}" + "\n" +
    $"Build: {RinBuildStamp.Branch}@{RinBuildStamp.CommitHash}" + "\n" +
    $"Time: {RinBuildStamp.BuildTime}" + "\n\n" +
    $"Konata.Core: {CoreBuildStamp.Version} {CoreBuildStamp.Branch}@{CoreBuildStamp.CommitHash}" + "\n" +
    @"Powered by Konata (C)" + "\n" +
    "\n"
    );
        Logger.Info("\n\n\n\n");
        Logger.Info("Initializing Bot.");
        BotManager.Instance.InitBot();
        Logger.Info("Initializing Database.");
        DatabaseManager.Instance.OpenConnection();
        Logger.Info("Loading CommandSets.");
        CommandManager.Instance.LoadCommandSets();

        Logger.Info("Logging in.");
        var result = BotManager.Instance.Bot.Login().Result;
        if (result)
        {
            Logger.Info("Bot online.");
        }
        else
        {
            Logger.Info("Bot login failed.");
            Environment.Exit(-1);
        }

        return 0;
    }

}