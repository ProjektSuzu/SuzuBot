namespace RinBot.Core.Components.Messages.Models
{
    internal class TextChain : BaseChain
    {
        public string Content { get; private set; }
        private TextChain(string content) : base(ChainType.Text, ChainMode.Multiple)
        {
            Content = content;
        }

        public static TextChain Create(string content)
            => new TextChain(content);
    }
}
