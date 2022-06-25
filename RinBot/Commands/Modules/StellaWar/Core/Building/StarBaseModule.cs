using RinBot.Commands.Modules.StellaWar.Core.War;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Commands.Modules.StellaWar.Core.Building
{
    internal abstract class StarBaseModule
    {
        /// <summary>
        /// 模块唯一ID
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// 模块是否唯一 (只能修一个)
        /// </summary>
        public bool Singleton { get; set; }
        /// <summary>
        /// 模块维护内存耗费 KB
        /// </summary>
        public int MaintainCostKB { get; set; }

        public abstract void Encounter(AggressiveWar battle);
    }
}
