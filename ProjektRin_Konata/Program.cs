using Konata.Core.Message;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ProjektRin;

public static class Program
{
    private static CommandLineInterface _cli = CommandLineInterface.Instance;
    private static string TAG = "Main";

    public static int Main()
    {
        _cli.Print(
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
    $"ProjektRin" + "\n" +
    @"Powered by Konata (C)" + "\n" +
    "\n"
    );
        _cli.Info(TAG, $"Current Dir: {AppDomain.CurrentDomain.BaseDirectory}");
        _cli.Info(TAG, "Initializing Bot.");
        BotManager.Instance.InitBot();
        _cli.Info(TAG, "Logging in.");

        var maxAttempt = 5;

        for (var i = 0; i < maxAttempt; i++)
        {
            if (BotManager.Instance.LoginBot())
            {
                break;
            }
        }

        uint devGroupUin = 644504300;
        _ = BotManager.Instance.Bot.SendGroupMessage(devGroupUin, new MessageBuilder("已从远端Git库拉取更改并自动构建\n" +
            $"UTC {DateTime.Now.ToUniversalTime():s}"));

        return 0;
    }

}