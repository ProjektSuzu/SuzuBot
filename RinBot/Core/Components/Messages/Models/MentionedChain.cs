namespace RinBot.Core.Components.Messages.Models
{
    internal class MentionedChain : BaseChain
    {
        public string TargetId { get; private set; }
        private MentionedChain(string targetId) : base(ChainType.Mentioned, ChainMode.Multiple)
        {
            TargetId = targetId;
        }

        public static MentionedChain Create(string targetID)
            => new MentionedChain(targetID);
    }
}
