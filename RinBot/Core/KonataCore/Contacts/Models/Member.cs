using Konata.Core.Common;
using Konata.Core.Message;

namespace RinBot.Core.KonataCore.Contacts.Models
{
    internal class Member : BotContact
    {
        public string NickName { get; internal set; }
        public string SpecialTitle { get; internal set; }
        public RoleType Role { get; internal set; }

        public bool IsAdmin { get => Role >= RoleType.Admin; }

        internal Member(string name, uint uin) : base(name, uin)
        {

        }

        public override async Task<bool> SendMessage(MessageChain chains)
        {
            throw new NotImplementedException();
        }
    }
}
