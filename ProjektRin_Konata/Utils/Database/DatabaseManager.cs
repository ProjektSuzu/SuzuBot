using NLog;
using ProjektRin.Components;
using ProjektRin.Utils.Database.Tables;
using SQLite;

namespace ProjektRin.Utils.Database
{
    internal class DatabaseManager
    {
        private static readonly DatabaseManager _instance = new();
        private DatabaseManager()
        {
        }
        public static DatabaseManager Instance => _instance;

        private static readonly string dbPath = Path.Combine(BotManager.rootPath, "database/rin.db");
        public SQLiteConnection dbConnection;

        private static readonly string TAG = "DBMGR";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public bool OpenConnection()
        {
            Logger.Info($"Connecting to database: {dbPath}.");
            if (!File.Exists(dbPath))
            {
                Logger.Error("Database not found.");
                return false;
            }
            dbConnection = new SQLiteConnection(dbPath);
            dbConnection.CreateTable<UserInfo>();
            Logger.Info("Database connected.");
            return true;
        }

    }
}
