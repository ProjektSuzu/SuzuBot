using SQLite;

namespace RinBot.Utils.Database.Tables
{
    public static class UserInfoManager
    {
        private static readonly SQLiteConnection db = DatabaseManager.Instance.dbConnection;

        public static int LevelToExp(int level)
        {
            return level * (level + 5) * 10;
        }

        public static string CoinToString(uint coin)
        {
            if (coin < 1000)
            {
                return $"{coin} KB";
            }
            else if (coin < 1000000)
            {
                return $"{(float)coin / 1000:f3} MB";
            }
            else if (coin < 1000000000)
            {
                return $"{(float)coin / 1000000:f3} GB";
            }
            else
            {
                return $"{(float)coin / 1000000000:f3} TB";
            }
        }

        public static bool UpdateUserInfo(UserInfo info)
        {
            int result = db
                .Update(info);
            return result > 0;
        }

        public static UserInfo? GetUserInfo(uint uin, bool create = true)
        {
            List<UserInfo>? result = db
                .Table<UserInfo>()
                .Where(t => t.uin == uin).ToList();
            if (result.Count == 0)
            {
                if (create)
                {
                    UserInfo? record = new UserInfo { uin = uin, coin = 0, exp = 0, level = 1, lastSign = new DateTime(), isBanned = false };
                    db.Insert(record);
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
    }

    [Table("T_USER_INFO")]
    public class UserInfo
    {
        [PrimaryKey]
        [Column("uin")]
        public uint uin { get; set; }

        [Column("coin")]
        public uint coin { get; set; }

        [Column("level")]
        public int level { get; set; }

        [Column("exp")]
        public int exp { get; set; }

        [Column("last_sign")]
        public DateTime lastSign { get; set; }

        [Column("is_banned")]
        public bool isBanned { get; set; }
    }
}
