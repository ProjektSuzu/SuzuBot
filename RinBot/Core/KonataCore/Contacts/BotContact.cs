using Konata.Core.Message;

namespace RinBot.Core.KonataCore.Contacts
{
    internal abstract class BotContact
    {
        public string Name { get; internal set; }
        public uint Uin { get; internal set; }

        public abstract Task<bool> SendMessage(MessageChain chains);
        public abstract Task<bool> SendPoke();

        protected BotContact(string name, uint uin)
        {
            Name = name;
            Uin = uin;
        }
    }
}
