using RinBot.Core.Component.Event;
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

        [Ignore]
        public string MemoryStr
        {
            get
            {
                if (Math.Abs(Memory) < 1_000)
                    return $"{Memory} KB";
                else if (Math.Abs(Memory) < 1_000_000)
                    return $"{(float)Memory / 1_000:0.000} MB";
                else if (Math.Abs(Memory) < 1_000_000_000)
                    return $"{(float)Memory / 1_000_000:0.000} GB";
                else
                    return $"{(float)Memory / 1_000_000_000:0.000} TB";
            }
        }
    }
}
