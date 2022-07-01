using SQLite;

namespace RinBot.Core.Component.Database
{
    [Table("T_QQ_GROUP_LIST")]
    internal class QQGroupInfo
    {
        [PrimaryKey]
        [Column("group_id")]
        public uint GroupId { get; set; }

        [Column("inviter_id")]
        public uint InviterId { get; set; }

        [Column("disable_modules")]
        public string DisableModules { get; set; }

        [Column("white_listed")]
        public bool WhiteListed { get; set; }

        [Column("black_listed")]
        public bool BlackListed { get; set; }

    }
}
