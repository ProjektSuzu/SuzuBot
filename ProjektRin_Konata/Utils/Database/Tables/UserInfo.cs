using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektRin.Utils.Database.Tables
{
    public static class UserInfoManager
    {
        private static SQLiteConnection _db = DatabaseManager.Instance.dbConnection;

        public static int LevelToExp(int level)
        {
            return level * (level + 5) * 10;
        }

        public static string CoinToString(uint coin)
        {
            if (coin < 1000)
            {
                return $"{coin} Byte";
            }
            else if (coin < 1000000)
            {
                return $"{(float)coin / 1000:f3} KB";
            }
            else if (coin < 1000000000)
            {
                return $"{(float)coin / 1000000:f3} MB";
            }
            else
            {
                return $"{(float)coin / 1000000000:f3} GB";
            }
        }

        public static bool UpdateUserInfo(UserInfo info)
        {
            var result = _db
                .Update(info);
            return result > 0;
        }

        public static UserInfo? GetUserInfo(uint uin, bool create = true)
        {
            var result = _db
                .Table<UserInfo>()
                .Where(t => t.uin == uin).ToList();
            if (result.Count == 0)
            {
                if (create)
                {
                    var record = new UserInfo { uin = uin, coin = 0, exp = 0, level = 1, lastSign = new DateTime() };
                    _db.Insert(record);
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

    }
}
