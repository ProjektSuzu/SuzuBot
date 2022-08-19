namespace RinBot.Core.Components.Messages.Models
{
    internal class ReplyChain : BaseChain
    {
        public object TargetMessageEvent { get; private set; }
        private ReplyChain(object targetMessage) : base(ChainType.Reply, ChainMode.Singletag)
        {
            TargetMessageEvent = targetMessage;
        }

        public static ReplyChain Create(object targetMessage)
            => new ReplyChain(targetMessage);
    }
}
