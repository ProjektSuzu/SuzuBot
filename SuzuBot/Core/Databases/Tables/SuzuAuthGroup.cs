using SQLite;

#pragma warning disable CS8618

namespace SuzuBot.Core.Databases.Tables;

[Table("t_auth_group")]
internal class SuzuAuthGroup
{
    [Column("auth_group")]
    public string AuthGroup { get; set; }
    [Column("priority")]
    public byte Priority { get; set; }
}
