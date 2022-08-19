using NLog;
using RinBot.BuildStamp;
using RinBot.Core;
using System.Runtime.InteropServices;

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
            Console.WriteLine("RinBot Copyright (C) 2020 AkulaKirov\n");

            Logger.Info($"RinBot-{RinBotBuildStamp.Version}");
            Logger.Info($"CommitHash: {RinBotBuildStamp.CommitHash}@{RinBotBuildStamp.Branch}");
            Logger.Info($"Running on: {RuntimeInformation.RuntimeIdentifier} with {RuntimeInformation.FrameworkDescription}");

            GlobalScope.BootStrap();

            Logger.Info("Program startup complete.");
            return 0;
        }
    }
}
