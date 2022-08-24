using Konata.Core.Interfaces.Api;
using Konata.Core.Message;

namespace RinBot.Core.KonataCore.Contacts.Models
{
    internal class Friend : BotContact
    {
        public string Remark { get; internal set; }
        internal Friend(string name, uint uin) : base(name, uin)
        {

        }

        public override async Task<bool> SendMessage(MessageChain chains)
        {
            return await GlobalScope.KonataBot.Bot.SendFriendMessage(Uin, chains);
        }
        public override async Task<bool> SendPoke()
        {
            return await GlobalScope.KonataBot.Bot.SendFriendPoke(Uin);
        }
    }
}
