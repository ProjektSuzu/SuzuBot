using RinBot.Utils.Database;
using SQLite;

namespace RinBot.Commands.Modules.Arcaea
{
    internal class ArcUserInfo
    {
        private static readonly string dbPath = Path.Combine(DatabaseManager.DbPath, "arcaea.db");
        public static SQLiteConnection dbConnection = new(dbPath);

        #region 单例模式
        private static ArcUserInfo instance;
        private ArcUserInfo() { }
        public static ArcUserInfo Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ArcUserInfo();
                    dbConnection.CreateTable<ArcaeaUserInfo>();
                    dbConnection.CreateTable<ArcaeaBindInfo>();
                }
                return instance;
            }
        }
        #endregion

        public ArcaeaUserInfo GetInfoByUin(uint uin)
        {
            var bind = dbConnection.Table<ArcaeaBindInfo>().FirstOrDefault(x => x.Uin == uin);
            if (bind != null)
                return dbConnection.Table<ArcaeaUserInfo>().FirstOrDefault(x => x.UserCode == bind.UserCode);
            else
                return null;
        }

        public ArcaeaBindInfo GetBindByUin(uint uin)
        {
            return dbConnection.Table<ArcaeaBindInfo>().FirstOrDefault(x => x.Uin == uin);
        }

        public ArcaeaUserInfo GetByUserCode(string usercode)
        {
            return dbConnection.Table<ArcaeaUserInfo>().FirstOrDefault(x => x.UserCode == usercode);
        }

        public bool UpdateBind(ArcaeaBindInfo bindInfo)
        {
            return dbConnection.InsertOrReplace(bindInfo) > 0;
        }

        public bool RemoveBind(uint uin)
        {
            return dbConnection.Delete<ArcaeaBindInfo>(uin) > 0;
        }

        public bool UpdateUserInfo(ArcaeaUserInfo userInfo)
        {
            return dbConnection.InsertOrReplace(userInfo) > 0;
        }

    }

    [Table("T_USER_INFO")]
    internal class ArcaeaUserInfo
    {
        [Column("usercode")]
        public string UserCode { get; set; }
        [Column("username")]
        public string UserName { get; set; }
        [Column("ptt")]
        public int PTT { get; set; }
        [Column("b30json")]
        public string B30Json { get; set; }
        [Column("record_json")]
        public string RecordJson { get; set; }
        [Column("last_update")]
        public DateTime LastUpdate { get; set; }
    }

    [Table("T_BIND_INFO")]
    internal class ArcaeaBindInfo
    {
        [PrimaryKey]
        [Column("uin")]
        public uint Uin { get; set; }
        [Column("usercode")]
        public string UserCode { get; set; }
    }

    internal class PttRecord
    {
        public string Date { get; set; }
        public int PTT { get; set; }
    }
}
