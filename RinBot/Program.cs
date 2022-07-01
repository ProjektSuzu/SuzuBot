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
        public static void Main()
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
            if (!Directory.Exists(Global.configPath))
            {
                Logger.Info("Mkdir: config.");
                Directory.CreateDirectory(Global.configPath);
            }
            if (!Directory.Exists(Global.resourcePath))
            {
                Logger.Info("Mkdir: resource.");
                Directory.CreateDirectory(Global.resourcePath);
            }
            if (!Directory.Exists(Global.databasePath))
            {
                Logger.Info("Mkdir: database.");
                Directory.CreateDirectory(Global.databasePath);
            }

            Logger.Info($"Initializing modules.");
            CommandManager.Instance.RegisterCommands();

            KonataBot konataBot = KonataBot.Instance;
            konataBot.InitializeBot();
            Logger.Info($"All set, ready to take off.");

            if (!konataBot.LoginBot())
            {
                konataBot.Bot.Dispose();
                return;
            }

            Logger.Info("Program startup complete.");
            return;
        }
    }
}
