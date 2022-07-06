using NLog;
using RinBot.BuildStamp;
using RinBot.Core;
using RinBot.Core.Component.Command;
using RinBot.Core.KonataCore;

namespace RinBot
{
    internal class Program
    {
        private static Logger Logger = LogManager.GetLogger("BOOT");
        public static int Main()
        {
            Console.WriteLine(@"    ____  _       ____        __ ");
            Console.WriteLine(@"   / __ \(_)___  / __ )____  / /_");
            Console.WriteLine(@"  / /_/ / / __ \/ __  / __ \/ __/");
            Console.WriteLine(@" / _, _/ / / / / /_/ / /_/ / /_  ");
            Console.WriteLine(@"/_/ |_/_/_/ /_/_____/\____/\__/  ");
            Console.WriteLine(@"=================================");
            Console.WriteLine("RinBot  Copyright (C) 2020  AkulaKirov\n");

            Logger.Info($"RinBot-{RinBotBuildStamp.Version}");
            Logger.Info($"{RinBotBuildStamp.CommitHash.Substring(RinBotBuildStamp.CommitHash.Length - 8)}@{RinBotBuildStamp.Branch}");

            Logger.Info("Checking directory structure.");
            if (!Directory.Exists(Global.CONFIG_PATH))
            {
                Logger.Info("Mkdir: config.");
                Directory.CreateDirectory(Global.CONFIG_PATH);
            }
            if (!Directory.Exists(Global.RESOURCE_PATH))
            {
                Logger.Info("Mkdir: resource.");
                Directory.CreateDirectory(Global.RESOURCE_PATH);
            }
            if (!Directory.Exists(Global.DB_PATH))
            {
                Logger.Info("Mkdir: database.");
                Directory.CreateDirectory(Global.DB_PATH);
            }

            Logger.Info($"Initializing modules.");
            CommandManager.Instance.RegisterCommands();

            KonataBot konataBot = KonataBot.Instance;
            konataBot.InitializeBot();
            Logger.Info($"All set, ready to take off.");

            if (!konataBot.LoginBot())
            {
                Logger.Fatal("Bot login failed.");
                konataBot.Bot.Dispose();
                return 1;
            }

            Logger.Info("Program startup complete.");
            return 0;
        }
    }
}
