using RinBot.Core.Component.Permission;
using SQLite;

namespace RinBot.Core.Component.Database
{
    [Table("T_QQ_USER_INFO")]
    internal class QQUserInfo
    {
        [PrimaryKey]
        [Column("user_id")]
        public uint UserId { get; set; }

        [Column("user_role")]
        public UserRole UserRole { get; set; }

        [Column("exp")]
        public long Exp { get; set; }

        [Column("memory")]
        public long Memory { get; set; }
    }
}
