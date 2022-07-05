using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
