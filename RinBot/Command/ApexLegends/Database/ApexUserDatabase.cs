using SQLite;

namespace RinBot.Command.ApexLegends.Database
{
    internal class ApexUserDatabase
    {
        public ApexUserDatabase()
        {
            dbConnection = new(APEX_USER_DB_PATH);
            dbConnection.CreateTableAsync<ApexBindInfo>();
        }
        private static string APEX_USER_DB_PATH => Path.Combine(ApexModule.DATABASE_DIR_PATH, "apex.db");
        private SQLiteAsyncConnection dbConnection;
        public SQLiteAsyncConnection DBConnection
            => dbConnection;

        public async Task<ApexBindInfo?> GetBindInfo(uint uin)
        {
            return await dbConnection.Table<ApexBindInfo>()
                .Where(x => x.Uin == uin)
                .FirstOrDefaultAsync();
        }

        public bool AddBindInfo(uint uin, string userId)
        {
            var info = new ApexBindInfo()
            {
                Uin = uin,
                UserId = userId
            };
            return dbConnection.InsertAsync(info).Result > 0;
        }
    }

    [Table("T_BIND_INFO")]
    internal class ApexBindInfo
    {
        [PrimaryKey]
        [Column("uin")]
        public uint Uin { get; set; }
        [Column("user_id")]
        public string UserId { get; set; }
    }
}
