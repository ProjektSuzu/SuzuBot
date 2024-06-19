using Microsoft.EntityFrameworkCore;
using SuzuBot.Commands.Attributes;

namespace SuzuBot.Database.Tables;

[PrimaryKey(nameof(Uin))]
internal class UserInfo
{
    public uint Uin { get; set; }
    public uint Exp { get; set; }
    public uint Coin { get; set; }
    public Permission Permission { get; set; }
}
