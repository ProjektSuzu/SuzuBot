using System.Text.Json;
using SQLite;
using SuzuBot.Utils;

namespace SuzuBot.Core.Tables;
[Table("t_group_info")]
public class SuzuGroupInfo
{
    [PrimaryKey]
    [Column("uin")]
    public uint Uin { get; set; }

    [Column("muted")]
    public bool Muted { get; set; } = false;

    [Column("white_listed")]
    public bool WhiteListed { get; set; } = false;

    [Column("modules")]
    public string ModulesJson { get; set; } = "[]";

    [Ignore]
    public List<string> Modules
    {
        get
        {
            return ModulesJson.DeserializeJson<List<string>>();
        }

        set
        {
            ModulesJson = JsonSerializer.Serialize(value);
        }
    }
}
