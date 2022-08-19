using RinBot.Core.Components.Event.Models;

namespace RinBot.Core.Components.Messages.Models
{
    internal class MultiMsgChain : BaseChain
    {
        public List<MessageEvent> Messages { get; private set; }
        private MultiMsgChain() : base(ChainType.MultiMsg, ChainMode.Singleton)
        {
            Messages = new();
        }

        public void AddMessage(string senderId, string senderName, RinMessageChain chains)
        {
            Messages.Add(
                new MessageEvent(
                    null,
                    null,
                    0,
                    0,
                    senderId,
                    senderName,
                    null,
                    chains,
                    chains.ToString()
                )
                );
        }
    }
}
