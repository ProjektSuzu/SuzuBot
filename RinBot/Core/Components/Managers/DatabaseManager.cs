using RinBot.Core.Components.Databases.Tables;
using SQLite;

namespace RinBot.Core.Components.Managers
{
    internal class DatabaseManager
    {
        #region Singleton
        public static DatabaseManager Instance = new Lazy<DatabaseManager>(() => new DatabaseManager()).Value;
        private DatabaseManager()
        {

        }
        #endregion
        public static readonly string rinDBPath = Path.Combine(GlobalScope.DB_DIR_PATH, "rin.db");
        private SQLiteAsyncConnection dbConnection;
        public SQLiteAsyncConnection DBConnection
            => dbConnection;
        public void OnInit()
        {
            dbConnection = new(rinDBPath);
            dbConnection.CreateTableAsync<ModuleInfo>();
            dbConnection.CreateTableAsync<QQUserInfo>();
        }

        public SQLiteAsyncConnection GetConnection(string fileName)
        {
            return new SQLiteAsyncConnection(Path.Combine(GlobalScope.DB_DIR_PATH, fileName));
        }
    }
}
