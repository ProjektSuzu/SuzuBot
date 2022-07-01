namespace RinBot.Core.Component.Message.Model
{
    internal abstract class BaseChain
    {
        public enum ChainType
        {
            Text,
            Image,
            Reply,
            At,
        }
        public enum ChainMode
        {
            Multiple,
            Singleton,
            Singletag
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
