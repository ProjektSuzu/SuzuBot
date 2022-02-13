using System.Reflection;

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

        _cli.Info(TAG, "Initializing Bot.");
        BotManager.Instance.InitBot();
        _cli.Info(TAG, "Logging in.");
        BotManager.Instance.LoginBot();


        return 0;
    }

}