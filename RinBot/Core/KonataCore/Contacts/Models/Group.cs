using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Core.KonataCore.Contacts.Models
{
    internal class Group : BotContact
    {
        public Member Owner { get; internal set; }
        public List<Member> Admins { get; internal set; }
        public uint MemberCount { get; internal set; }
        public uint MaxMemberCount { get; internal set; }
        public uint TotalMuted { get; internal set; }
        public uint SelfMuted { get; internal set; }
        public Dictionary<uint, Member> Members { get; internal set; }

        internal Group(string name, uint uin) : base(name, uin)
        {

        }
    }
}
