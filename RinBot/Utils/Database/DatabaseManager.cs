using NLog;
using ProjektRin.Core.Components;
using ProjektRin.Utils.Database.Tables;
using SQLite;

namespace ProjektRin.Utils.Database
{
    internal class DatabaseManager
    {
        #region 单例模式
        private static DatabaseManager instance;
        private DatabaseManager()
        {
            if (!Directory.Exists(dbPath))
                Directory.CreateDirectory(dbPath);
        }
        public static DatabaseManager Instance
        {
            get
            {
                if (instance == null) instance = new();
                return instance;
            }
        }
        #endregion

        private static readonly string dbPath = Path.Combine(BotManager.rootPath, "database");
        private static readonly string rinDbPath = Path.Combine(dbPath, "rin.db");
        public SQLiteConnection dbConnection;

        private static readonly string TAG = "DBMGR";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public bool OpenConnection()
        {
            Logger.Info($"Connecting to database: {rinDbPath}.");
            if (!File.Exists(rinDbPath))
            {
                Logger.Error("Database not found.");
                return false;
            }
            dbConnection = new SQLiteConnection(rinDbPath);
            dbConnection.CreateTable<UserInfo>();
            Logger.Info("Database connected.");
            return true;
        }

    }
}
