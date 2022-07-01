namespace RinBot.Core.Component.Message.Model
{
    internal class ReplyChain : BaseChain
    {
        public object TargetMessageEvent { get; private set; }
        public ReplyChain(object targetMessage) : base(ChainType.Reply, ChainMode.Singletag)
        {
            TargetMessageEvent = targetMessage;
        }

        public static ReplyChain Create(object targetMessage)
            => new ReplyChain(targetMessage);
    }
}
