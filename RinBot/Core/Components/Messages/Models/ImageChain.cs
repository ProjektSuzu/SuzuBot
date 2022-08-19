using RestSharp;

namespace RinBot.Core.Components.Messages.Models
{
    internal class ImageChain : BaseChain
    {
        public byte[] Bytes { get; private set; }

        public string ImageHash { get; private set; }

        private ImageChain(byte[] bytes, string hash = "") : base(ChainType.Image, ChainMode.Multiple)
        {
            Bytes = bytes;
            ImageHash = hash;
        }

        private async Task<byte[]> Download()
        {
            var client = new RestClient($"https://gchat.qpic.cn/gchatpic_new/0/0-0-{ImageHash}/0");
            var request = new RestRequest()!;
            return (await client.ExecuteAsync(request)).RawBytes ?? new byte[] { };
        }

        public static ImageChain Create(byte[] bytes)
            => new ImageChain(bytes);

        public static ImageChain Create(string hash)
            => new ImageChain(new byte[] { }, hash);
    }
}
