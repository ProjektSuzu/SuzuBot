namespace RinBot.Core.Component.Message.Model
{
    internal class ImageChain : BaseChain
    {
        public byte[] Bytes { get; private set; }
        public ImageChain(byte[] bytes) : base(ChainType.Image, ChainMode.Multiple)
        {
            Bytes = bytes;
        }

        public static ImageChain Create(byte[] bytes)
            => new ImageChain(bytes);
    }
}
