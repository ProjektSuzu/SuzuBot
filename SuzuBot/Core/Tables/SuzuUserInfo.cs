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
    [Column("exp")]
    public long Exp { get; set; } = 0;
    [Column("level")]
    public long Level { get; set; } = 0;
    [Column("auth_group")]
    public AuthGroup AuthGroup { get; set; } = AuthGroup.User;

    [Ignore]
    public long NextLevelExp
    {
        get
        {
            long level = Level + 1;
            return 30 * level * (level + 2) - 80 - Exp;
        }
    }

    public bool GiveExp(uint exp)
    {
        bool upgrade = false;
        Exp += exp;
        while (Exp >= NextLevelExp)
        {
            upgrade = true;
            Level++;
        }

        return upgrade;
    }
}