using RinBot.Command.ApexLegends.Database;
using RinBot.Command.Arcaea.Database;
using RinBot.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Command.ApexLegends
{
    internal class ApexModule
    {
        internal static readonly string RESOURCE_DIR_PATH = Path.Combine(GlobalScope.RESOURCE_DIR_PATH, "AkulaKirov.ApexLegends");
        internal static readonly string DATABASE_DIR_PATH = Path.Combine(RESOURCE_DIR_PATH, "database");

        internal static ApexUserDatabase ApexUserDatabase
            => new ApexUserDatabase();
    }
}
