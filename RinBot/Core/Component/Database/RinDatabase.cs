using SQLite;

namespace RinBot.Core.Component.Database
{
    internal class RinDatabase
    {
        #region Singleton
        private static RinDatabase instance;
        private RinDatabase()
        {

        }
        public static RinDatabase Instance
        {
            get
            {
                if (instance == null) instance = new();
                return instance;
            }
        }
        #endregion
        public static readonly string rinDBPath = Path.Combine(Global.DB_PATH, "rin.db");
        public SQLiteConnection dbConnection = new(rinDBPath);
    }
}
