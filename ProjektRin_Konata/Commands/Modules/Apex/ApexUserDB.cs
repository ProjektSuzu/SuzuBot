using ProjektRin.Components;
using SQLite;

namespace ProjektRin.Commands.Modules.Apex
{
    internal class ApexUserDB
    {
        private static readonly string dbPath = Path.Combine(BotManager.rootPath, "database/apexUser.db");
        public static SQLiteConnection dbConnection = new(dbPath);

        #region 单例模式
        private static ApexUserDB instance;
        public static ApexUserDB Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ApexUserDB();
                    dbConnection.CreateTable<ApexUserInfo>();
                }
                return instance;
            }
        }
        private ApexUserDB()
        {

        }
        #endregion

        public ApexUserInfo? GetByUin(uint uin)
        {
            return dbConnection.Table<ApexUserInfo>().FirstOrDefault(x => x.Uin == uin);
        }

        public bool Insert(ApexUserInfo userInfo)
        {
            return dbConnection.Insert(userInfo) > 0;
        }

        public bool Remove(uint uin)
        {
            return dbConnection.Delete<ApexUserInfo>(uin) > 0;
        }

        public bool Update(ApexUserInfo userInfo)
        {
            return dbConnection.Update(userInfo) > 0;
        }

        [Table("T_USER_INFO")]
        internal class ApexUserInfo
        {
            [PrimaryKey]
            [Column("uin")]
            public uint Uin { get; set; }
            [Column("userId")]
            public string UserId { get; set; }
        }
    }
}
