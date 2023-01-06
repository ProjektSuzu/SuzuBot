using Konata.Core;
using Konata.Core.Interfaces.Api;

namespace SuzuBot.Core.Contacts;
public class Friend : Contact
{
    public string Remark { get; set; }

    public static Friend? GetSuzuFriend(Bot bot, uint id)
    {
        var friend = bot.GetFriendList().Result
            .Where(x => x.Uin == id)
            .FirstOrDefault();
        if (friend is null)
            return null;

        return new()
        {
            Id = friend.Uin,
            Name = friend.Name,
            AvatarUrl = friend.AvatarUrl,
            Remark = friend.Remark,
        };
    }
}
