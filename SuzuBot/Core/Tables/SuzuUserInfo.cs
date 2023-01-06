using SQLite;
using SuzuBot.Core.Attributes;

namespace SuzuBot.Core.Tables;

[Table("t_user_info")]
public class SuzuUserInfo
{
    [PrimaryKey]
    [Column("uin")]
    public uint Uin { get; set; }
    [Column("coin")]
    public long Coin { get; set; } = 0;
    [Column("favor")]
    public long Favor { get; set; } = 0;
    [Column("auth_group")]
    public AuthGroup AuthGroup { get; set; } = AuthGroup.User;
}