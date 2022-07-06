namespace RinBot.Core.Component.Message.Model
{
    internal class AtChain : BaseChain
    {
        public string TargetID { get; private set; }
        public AtChain(string targetID) : base(ChainType.At, ChainMode.Multiple)
        {
            TargetID = targetID;
        }

        public static AtChain Create(string targetID)
            => new AtChain(targetID);
    }
}
