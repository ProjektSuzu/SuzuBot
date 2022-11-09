using SQLite;

namespace SuzuBot.Core.Databases.Tables;

[Table("t_user_info")]
internal class SuzuUserInfo
{
    [PrimaryKey]
    [Column("uin")]
    public uint Uin { get; set; }
    [Column("coin")]
    public long Coin { get; set; } = 0;
    [Column("favor")]
    public long Favor { get; set; } = 0;
    [Column("auth_group")]
    public string AuthGroup { get; set; } = "user";
}