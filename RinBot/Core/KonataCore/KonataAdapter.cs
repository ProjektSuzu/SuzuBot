using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using RinBot.Core.KonataCore.Contacts.Models;
using RinBot.Core.KonataCore.Events;

namespace RinBot.Core.KonataCore
{
    internal class KonataAdapter
    {
        #region Singleton
        public static KonataAdapter Instance = new Lazy<KonataAdapter>(() => new KonataAdapter()).Value;
        private KonataAdapter()
        {

        }
        #endregion

        public MessageEventArgs WarpMessageEvent(GroupMessageEvent groupMessageEvent)
        {
            var group = GetGroup(groupMessageEvent.GroupUin).Result;
            var member = GetMember(groupMessageEvent.MemberUin, group).Result;

            return new MessageEventArgs()
            {
                Subject = group,
                Sender = member,
                Message = groupMessageEvent.Message,
            };
        }

        public MessageEventArgs WarpMessageEvent(FriendMessageEvent friendMessageEvent)
        {
            var friend = GetFriend(friendMessageEvent.FriendUin).Result;

            return new MessageEventArgs()
            {
                Subject = friend,
                Sender = friend,
                Message = friendMessageEvent.Message,
            };
        }

        public async Task<Friend?> GetFriend(uint uin)
        {
            var friend = GlobalScope.KonataBot.Bot.GetFriendList().Result.FirstOrDefault(x => x.Uin == uin);
            if (friend == null)
            {
                return null;
            }
            else
            {
                return new(friend.Name, friend.Uin)
                {
                    Remark = friend.Remark
                };
            }
        }
        public async Task<Group?> GetGroup(uint uin)
        {
            var group = GlobalScope.KonataBot.Bot.GetGroupList().Result.FirstOrDefault(x => x.Uin == uin);
            if (group == null)
            {
                return null;
            }
            else
            {
                var members = GlobalScope.KonataBot.Bot.GetGroupMemberList(uin).Result;
                var owner = members.First(x => x.Uin == group.OwnerUin);
                var admins = members.Where(x => group.AdminUins.Contains(x.Uin));
                var memberDict = new Dictionary<uint, Member>();
                foreach (var member in members)
                {
                    memberDict.Add(member.Uin, new Member(member.Name, member.Uin)
                    {
                        NickName = member.NickName,
                        SpecialTitle = member.SpecialTitle,
                        Role = member.Role,
                    });
                }
                return new(group.Name, group.Uin)
                {
                    Owner = new(owner.Name, owner.Uin)
                    {
                        NickName = owner.NickName,
                        SpecialTitle = owner.SpecialTitle,
                        Role = owner.Role,
                    },
                    Admins = admins.Select(x => new Member(x.Name, x.Uin)
                    {
                        NickName = x.NickName,
                        SpecialTitle = x.SpecialTitle,
                        Role = x.Role
                    }).ToList(),
                    MemberCount = group.MemberCount,
                    MaxMemberCount = group.MaxMemberCount,
                    TotalMuted = group.Muted,
                    SelfMuted = group.MutedMe,
                    Members = memberDict,
                };
            }
        }
        public async Task<Member?> GetMember(uint memberUin, uint groupUin)
        {
            var group = await GetGroup(groupUin);
            return group == null ? null : await GetMember(memberUin, group);
        }
        public async Task<Member?> GetMember(uint memberUin, Group group)
        {
            if (group.Members.TryGetValue(memberUin, out var member))
            {
                return new(member.Name == "" ? member.NickName : member.Name, member.Uin)
                {
                    NickName = member.NickName,
                    SpecialTitle = member.SpecialTitle,
                    Role = member.Role,
                };
            }
            else
            {
                return null;
            }
        }
    }
}
