using Konata.Core;
using Konata.Core.Common;
using Konata.Core.Interfaces.Api;

namespace SuzuBot.Core.Contacts;
public class Group : Contact
{
    public Member Owner { get; set; }
    public IEnumerable<Member> Admins { get; set; }
    public IEnumerable<Member> Members { get; set; }
    public bool IsGroupMuted { get; set; }
    public bool IsBotMuted { get; set; }

    public static Group? GetSuzuGroup(Bot bot, uint id)
    {
        var botGroup = bot.GetGroupList().Result
            .Where(x => x.Uin == id)
            .FirstOrDefault();
        if (botGroup is null)
            return null;

        Member ToSuzuMember(BotMember member)
        {
            return new()
            {
                Id = member.Uin,
                Name = member.NickName,
                AvatarUrl = member.AvatarUrl,
                GroupId = id,
                IsAdmin = member.Role >= RoleType.Admin,
                IsOwner = member.Role >= RoleType.Owner,
                MuteSeconds = member.MuteTimestamp
            };
        }

        var botMembers = bot.GetGroupMemberList(botGroup.Uin).Result;
        var owner = botMembers
            .Where(x => x.Role == RoleType.Owner)
            .Select(ToSuzuMember)
            .FirstOrDefault();
        var admins = botMembers
            .Where(x => x.Role >= RoleType.Admin)
            .Select(ToSuzuMember);
        var members = botMembers
            .Select(ToSuzuMember);

        return new()
        {
            Id = id,
            Name = botGroup.Name,
            AvatarUrl = botGroup.AvatarUrl,
            Owner = owner,
            Admins = admins,
            Members = members,
            IsGroupMuted = botGroup.Muted > 0,
            IsBotMuted = botGroup.MutedMe > 0
        };
    }
}
