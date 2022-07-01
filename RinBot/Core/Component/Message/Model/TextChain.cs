namespace RinBot.Core.Component.Message.Model
{
    internal class TextChain : BaseChain
    {
        public string Content { get; private set; }
        public TextChain(string content) : base(ChainType.Text, ChainMode.Multiple)
        {
            Content = content;
        }

        public static TextChain Create(string content)
            => new TextChain(content);
    }
}
