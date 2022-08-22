using Konata.Core.Interfaces.Api;
using RinBot.Core.KonataCore.Contacts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    Members = memberDict;
                };
            }
        }
    }
}
