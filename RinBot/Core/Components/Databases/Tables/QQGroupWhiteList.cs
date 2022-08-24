using SQLite;

namespace RinBot.Core.Components.Databases.Tables
{
    [Table("T_QQ_GROUP_WHITELIST")]
    internal class QQGroupWhiteList
    {
        [PrimaryKey]
        [Column("uin")]
        public uint Uin { get; set; }
    }
}
