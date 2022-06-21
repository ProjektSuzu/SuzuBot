using RinBot.Core.Components;
using RinBot.Utils.Database;
using RinBot.Utils.Database.Tables;
using SQLite;

namespace RinBot.Commands.Modules.MemoryWar
{
    internal class MemoryDB
    {
        private static readonly string dbPath = Path.Combine(DatabaseManager.DbPath, "memory_war.db");
        public SQLiteConnection dbConnection = new(dbPath);

        #region 单例模式
        private static MemoryDB instance;
        public static MemoryDB Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MemoryDB();
                }
                return instance;
            }
        }
        private MemoryDB()
        {
            dbConnection.CreateTable<MemoryInfo>();
        }
        #endregion

        public MemoryInfo? GetUserInfo(uint uin, bool create = true)
        {
            List<MemoryInfo>? result = dbConnection
                .Table<MemoryInfo>()
                .Where(t => t.uin == uin).ToList();
            if (result.Count == 0)
            {
                if (create)
                {
                    MemoryInfo? record = new MemoryInfo { uin = uin, engineer = 0, attacker = 0, lastWar = new DateTime(1927, 8, 17), isProtected = false };
                    dbConnection.Insert(record);
                    return GetUserInfo(uin);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return result[0];
            }
        }

        public bool UpdateUserInfo(MemoryInfo info)
        {
            int result = dbConnection
                .Update(info);
            return result > 0;
        }
    }
    [Table("T_MEMORY_INFO")]

    internal class MemoryInfo
    {
        [PrimaryKey]
        [Column("uin")]
        public uint uin { get; set; }

        [Column("engineer")]
        public int engineer { get; set; }

        [Column("attacker")]
        public int attacker { get; set; }

        [Column("isProtected")]
        public bool isProtected { get; set; }

        [Column("lastWar")]
        public DateTime lastWar { get; set; }

        [Column("nextBuild")]
        public DateTime nextBuild { get; set; }
    }
}
