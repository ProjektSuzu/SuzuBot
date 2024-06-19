using Microsoft.EntityFrameworkCore;

namespace SuzuBot.Database.Tables;

[PrimaryKey(nameof(Id))]
internal class GroupRule
{
    public int Id { get; set; }
    public uint GroupUin { get; set; }
    public string CommandId { get; set; }
    public string Rule { get; set; }
}
