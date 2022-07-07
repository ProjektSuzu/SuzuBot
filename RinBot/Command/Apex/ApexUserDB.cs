using RinBot.Command.Arcaea;
using RinBot.Core.Component.Event;
using RinBot.Core;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Command.Apex
{
    internal class ApexUserDB
    {
        #region Singleton
        private static ApexUserDB instance;
        public static ApexUserDB Instance
        {
            get
            {
                if (instance == null) instance = new();
                return instance;
            }
        }
        private ApexUserDB()
        {
            dbConnection = new SQLiteConnection(DB_PATH);
            dbConnection.CreateTable<ApexBindInfo>();
        }
        #endregion
        SQLiteConnection dbConnection;
        private static readonly string DB_PATH = Path.Combine(Global.DB_PATH, "apex.db");

        public ApexBindInfo? GetBindInfo(string userId, EventSourceType userType)
        {
            var query = dbConnection.Table<ApexBindInfo>().Where(x => x.UserId == userId && x.UserType == userType);
            if (query.Count() <= 0)
            {
                return null;
            }
            return query.First();
        }

        public bool UpdateBindInfo(ApexBindInfo info)
        {
            return dbConnection.Update(info) > 0 || dbConnection.Insert(info) > 0;
        }

        public bool DeleteBindInfo(string userId, EventSourceType userType)
        {
            var info = GetBindInfo(userId, userType);
            if (info == null) return false;
            return dbConnection.Delete(info) > 0;
        }

        public bool DeleteBindInfo(ApexBindInfo info)
            => DeleteBindInfo(info.UserId, info.UserType);
    }

    [Table("T_BIND_INFO")]
    internal class ApexBindInfo
    {
        [PrimaryKey]
        [Column("user_id")]
        public string UserId { get; set; }
        [Column("user_type")]
        public EventSourceType UserType { get; set; }
        [Column("player_id")]
        public string PlayerId { get; set; }
        [Column("player_name")]
        public string PlayerName { get; set; }
    }
}
