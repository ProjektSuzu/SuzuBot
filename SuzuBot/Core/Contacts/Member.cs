using Konata.Core;
using Konata.Core.Common;
using Konata.Core.Interfaces.Api;

namespace SuzuBot.Core.Contacts;
public class Member : Contact
{
    public uint GroupId { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsOwner { get; set; }
    public long MuteSeconds { get; set; }

    public static Member? GetSuzuMember(Bot bot, uint groupId, uint memberId)
    {
        var member = bot.GetGroupMemberList(groupId).Result
            .Where(x => x.Uin == memberId)
            .FirstOrDefault();
        if (member is null)
            return null;

        return new()
        {
            Id = member.Uin,
            Name = member.NickName,
            AvatarUrl = member.AvatarUrl,
            GroupId = groupId,
            IsAdmin = member.Role >= RoleType.Admin,
            IsOwner = member.Role >= RoleType.Owner,
            MuteSeconds = member.MuteTimestamp
        };
    }
}
