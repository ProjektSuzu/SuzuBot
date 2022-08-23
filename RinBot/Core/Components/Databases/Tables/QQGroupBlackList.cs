using RinBot.Core.Components.Managers;
using SQLite;

namespace RinBot.Core.Components.Databases.Tables
{
    [Table("T_QQ_GROUP_BLACKLIST")]
    internal class QQGroupBlackList
    {
        [PrimaryKey]
        [Column("uin")]
        public uint Uin { get; set; }
        [Column("reason")]
        public string Reason { get; set; }
    }
}
