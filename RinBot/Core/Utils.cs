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
    }
}
