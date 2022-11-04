using SQLite;

namespace RinBot.Core.Databases.Tables;

[Table("t_auth_group")]
internal class RinAuthGroup
{
    [Column("auth_group")]
    public string AuthGroup { get; set; }
    [Column("priority")]
    public byte Priority { get; set; }
}
