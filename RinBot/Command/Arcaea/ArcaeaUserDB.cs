using RinBot.Core;
using RinBot.Core.Component.Event;
using SQLite;

namespace RinBot.Command.Arcaea
{
    internal class ArcaeaUserDB
    {
        #region Singleton
        private static ArcaeaUserDB instance;
        public static ArcaeaUserDB Instance
        {
            get
            {
                if (instance == null) instance = new();
                return instance;
            }
        }
        private ArcaeaUserDB()
        {
            dbConnection = new SQLiteConnection(DB_PATH);
            dbConnection.CreateTable<ArcaeaBindInfo>();
            dbConnection.CreateTable<ArcaeaPlayerInfo>();
        }
        #endregion
        SQLiteConnection dbConnection;
        private static readonly string DB_PATH = Path.Combine(Global.DB_PATH, "arcaea.db");

        public ArcaeaBindInfo? GetBindInfo(string userId, EventSourceType userType)
        {
            var query = dbConnection.Table<ArcaeaBindInfo>().Where(x => x.UserId == userId && x.UserType == userType);
            if (query.Count() <= 0)
            {
                return null;
            }
            return query.First();
        }

        public ArcaeaPlayerInfo? GetPlayerInfo(ArcaeaBindInfo bindInfo)
            => GetPlayerInfo(bindInfo.UserCode);

        public ArcaeaPlayerInfo? GetPlayerInfo(string userCode)
        {
            var query = dbConnection.Table<ArcaeaPlayerInfo>().Where(x => x.UserCode == userCode);
            if (query.Count() <= 0)
            {
                return null;
            }
            return query.First();
        }

        public bool UpdatePlayerInfo(string userCode, string userName)
        {
            return dbConnection.InsertOrReplace(new ArcaeaPlayerInfo() { UserCode = userCode, UserName = userName }) > 0;
        }

        public bool UpdateBindInfo(string userId, EventSourceType userType, string userCode)
        {
            return dbConnection.InsertOrReplace(new ArcaeaBindInfo() { UserId = userId, UserType = userType, UserCode = userCode }) > 0;
        }

        public bool DeleteBindInfo(string userId, EventSourceType userType)
        {
            var info = GetBindInfo(userId, userType);
            if (info == null) return false;
            return dbConnection.Delete(info) > 0;
        }

        public bool DeleteBindInfo(ArcaeaBindInfo info)
            => DeleteBindInfo(info.UserId, info.UserType);
    }

    [Table("T_BIND_INFO")]
    internal class ArcaeaBindInfo
    {
        [PrimaryKey]
        [Column("user_id")]
        public string UserId { get; set; }
        [Column("user_type")]
        public EventSourceType UserType { get; set; }
        [Column("user_code")]
        public string UserCode { get; set; }
    }

    [Table("T_PLAYER_INFO")]
    internal class ArcaeaPlayerInfo
    {
        [PrimaryKey]
        [Column("user_code")]
        public string UserCode { get; set; }
        [Column("user_name")]
        public string UserName { get; set; }
    }
}
