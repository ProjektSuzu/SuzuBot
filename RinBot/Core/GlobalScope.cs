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

        // Managers
        public static DatabaseManager DatabaseManager
            => DatabaseManager.Instance;
        public static CommandManager CommandManager
            => CommandManager.Instance;
        public static PermissionManager PermissionManager
            => PermissionManager.Instance;

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

            // Initialize Managers
            Logger.Info("Initialize Managers.");
            DatabaseManager.OnInit();
            CommandManager.OnInit();
            PermissionManager.OnInit();

            // Initialize Bots
            KonataBot.InitBot();
        }
    }
}
