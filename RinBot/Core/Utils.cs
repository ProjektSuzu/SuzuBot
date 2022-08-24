using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Core
{
    internal static class Utils
    {
        public static DateTime GetUnixDateTimeSeconds(long timespan)
        {
            long ticks = timespan * 10000000;
            DateTime utc1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            utc1970.AddTicks(ticks);
            return utc1970;
        }

        public static DateTime GetUnixDateTimeMilliseconds(long timespan)
        {
            long ticks = timespan * 10000;
            DateTime utc1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            utc1970.AddTicks(ticks);
            return utc1970;
        }
    }
}
