using NLog;
using RinBot.Core.Components;
using RinBot.Utils.Database.Tables;
using SQLite;

namespace RinBot.Utils.Database
{
    internal class DatabaseManager
    {
        #region 单例模式
        private static DatabaseManager instance;
        private DatabaseManager()
        {
            if (!Directory.Exists(DbPath))
                Directory.CreateDirectory(DbPath);
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

        public static readonly string DbPath = Path.Combine(BotManager.rootPath, "databases");
        private static readonly string rinDbPath = Path.Combine(DbPath, "rin.db");
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
