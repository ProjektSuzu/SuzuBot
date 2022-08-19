namespace RinBot.Core.Components.Messages
{
    internal abstract class BaseChain
    {
        public enum ChainType : short
        {
            Text,
            Image,
            Reply,
            Mentioned,
            MultiMsg,
        }
        public enum ChainMode : short
        {
            Multiple,
            Singleton,
            Singletag,
        }

        public ChainType Type { get; protected set; }
        public ChainMode Mode { get; protected set; }

        protected BaseChain(ChainType type, ChainMode mode)
        {
            Type = type;
            Mode = mode;
        }
    }
}
