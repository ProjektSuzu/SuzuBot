using Konata.Core.Interfaces.Api;
using Konata.Core.Message;

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

        public override async Task<bool> SendMessage(MessageChain chains)
        {
            return await GlobalScope.KonataBot.Bot.SendGroupMessage(Uin, chains);
        }

        public override async Task<bool> SendPoke()
        {
            throw new InvalidOperationException();
        }
    }
}
