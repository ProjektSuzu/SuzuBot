using Konata.Core.Message.Model;
using RestSharp;

namespace RinBot.Core
{
    internal static class Utils
    {
        public static DateTime GetUnixDateTimeSeconds(long timespan)
        {
            long ticks = timespan * 10000000;
            DateTime utc1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return utc1970.AddTicks(ticks);
        }

        public static DateTime GetUnixDateTimeMilliseconds(long timespan)
        {
            long ticks = timespan * 10000;
            DateTime utc1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return utc1970.AddTicks(ticks);
        }

        public static byte[] Download(this ImageChain imageChain)
        {
            var url = $"https://gchat.qpic.cn/gchatpic_new/0/0-0-{imageChain.FileHash}/0";
            RestClient client = new(url);
            RestRequest request = new RestRequest();
            return client.Execute(request).RawBytes ?? Array.Empty<byte>();
        }
    }
}
