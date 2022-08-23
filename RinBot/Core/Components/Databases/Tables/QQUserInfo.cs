using RinBot.Core.Components.Managers;
using SQLite;

namespace RinBot.Core.Components.Databases.Tables
{
    [Table("T_QQ_USER_INFO")]
    internal class QQUserInfo
    {
        [PrimaryKey]
        [Column("uin")]
        public uint Uin { get; set; }

        [Column("level")]
        public UserPermission Level { get; set; }

        [Column("exp")]
        public long Exp { get; set; }

        [Column("coin")]
        public long Coin { get; set; }

        [Column("favor")]
        public long Favor { get; set; }

    }
}
