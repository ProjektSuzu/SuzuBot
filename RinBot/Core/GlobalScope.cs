using NLog;
using RinBot.Core.Components.Managers;
using RinBot.Core.KonataCore;
using SQLite;

namespace RinBot.Core
{
    internal static class GlobalScope
    {
        public static readonly string ROOT_DIR_PATH = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string CONFIG_DIR_PATH = Path.Combine(ROOT_DIR_PATH, "config");
        public static readonly string RESOURCE_DIR_PATH = Path.Combine(ROOT_DIR_PATH, "resource");
        public static readonly string DB_DIR_PATH = Path.Combine(ROOT_DIR_PATH, "database");

        // Bot
        public static KonataBot KonataBot
            => KonataBot.Instance;

        // Adapter
        public static KonataAdapter KonataAdapter
            => KonataAdapter.Instance;

        // Managers
        public static DatabaseManager DatabaseManager
            => DatabaseManager.Instance;
        public static CommandManager CommandManager
            => CommandManager.Instance;
        public static PermissionManager PermissionManager
            => PermissionManager.Instance;
        public static EventManager EventManager
            => EventManager.Instance;

        // DatabaseConnections
        public static SQLiteAsyncConnection RinDBAsyncConnection
            => DatabaseManager.DBConnection;

        private static Logger Logger = LogManager.GetLogger("BOOT");

        public static void BootStrap()
        {
            // Touch Directories
            Logger.Info("Touch Directories.");
            Directory.CreateDirectory(CONFIG_DIR_PATH);
            Directory.CreateDirectory(RESOURCE_DIR_PATH);
            Directory.CreateDirectory(DB_DIR_PATH);

            // Initialize Bots
            Logger.Info("Step 1/3: Initialize Bot(s).");
            KonataBot.InitBot();

            // Initialize Managers
            Logger.Info("Step 2/3: Initialize Manager(s).");
            DatabaseManager.OnInit();
            PermissionManager.OnInit();
            EventManager.OnInit();
            CommandManager.OnInit();

            // Login Bots
            Logger.Info("Step 3/3: Login Bot(s).");
            if (KonataBot.LoginBot())
            {
                Logger.Info("Bot Login Success.");
            }
            else
            {
                Logger.Fatal("Bot Login Failed.");
            }
            Logger.Info("Program startup complete.");
        }
    }
}
