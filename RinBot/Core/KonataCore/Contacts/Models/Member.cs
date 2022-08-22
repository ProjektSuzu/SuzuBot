using Konata.Core.Common;

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
    }
}
