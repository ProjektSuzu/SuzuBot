using RinBot.Utils.Database;
using SQLite;

namespace RinBot.Commands.Modules.Arcaea
{
    internal class ArcUserInfoDB
    {
        private static readonly string dbPath = Path.Combine(DatabaseManager.DbPath, "arcaea.db");
        public static SQLiteConnection dbConnection = new(dbPath);

        #region 单例模式
        private static ArcUserInfoDB instance;
        private ArcUserInfoDB() { }
        public static ArcUserInfoDB Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ArcUserInfoDB();
                    dbConnection.CreateTable<ArcaeaUserInfo>();
                }
                return instance;
            }
        }
        #endregion

        public ArcaeaUserInfo GetByUin(uint uin)
        {
            return dbConnection.Table<ArcaeaUserInfo>().FirstOrDefault(x => x.Uin == uin);
        }

        public ArcaeaUserInfo GetByUserCode(string usercode)
        {
            return dbConnection.Table<ArcaeaUserInfo>().FirstOrDefault(x => x.UserCode == usercode);
        }

        public ArcaeaUserInfo Insert(ArcaeaUserInfo userInfo)
        {
            dbConnection.Insert(userInfo);
            return userInfo;
        }

        public bool Remove(uint uin)
        {
            return dbConnection.Delete<ArcaeaUserInfo>(uin) > 0;
        }

        public bool Update(ArcaeaUserInfo userInfo)
        {
            return dbConnection.Update(userInfo) > 0;
        }

    }

    [Table("T_USER_INFO")]
    internal class ArcaeaUserInfo
    {
        [PrimaryKey]
        [Column("uin")]
        public uint Uin { get; set; }
        [Column("username")]
        public string UserName { get; set; }
        [Column("usercode")]
        public string UserCode { get; set; }
        [Column("b30json")]
        public string B30Json { get; set; }
        [Column("last_update")]
        public DateTime LastUpdate { get; set; }
    }
}
