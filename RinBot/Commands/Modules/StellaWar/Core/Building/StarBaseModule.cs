using RinBot.Commands.Modules.StellaWar.Core.Ship;
using RinBot.Commands.Modules.StellaWar.Core.War;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Commands.Modules.StellaWar.Core.Building
{
    [Table("T_MODULE_TECH")]
    internal class StarBaseModule
    {
        /// <summary>
        /// 模块唯一ID
        /// </summary>
        [Column("id")]
        public string ID { get; set; }
        /// <summary>
        /// 模块名称
        /// </summary>
        [Column("name")]
        public string Name { get; set; }
        /// <summary>
        /// 模块描述
        /// </summary>
        [Column("description")]
        public string Description { get; set; }
        /// <summary>
        /// 模块是否唯一 (只能修一个)
        /// </summary>
        [Column("singleton")]
        public bool Singleton { get; set; }
        /// <summary>
        /// 模块维护内存耗费 KB
        /// </summary>
        [Column("maintain_cost")]
        public int MaintainCostKB { get; set; }
        /// <summary>
        /// 舰船建造耗时分钟数
        /// </summary>
        [Column("build_time_minute")]
        public float BuildTimeMinute { get; set; }
        /// <summary>
        /// 舰船建造要求内存数 KB
        /// </summary>
        [Column("build_cost_kb")]
        public int BuildCostKB { get; set; }
        /// <summary>
        /// 模块解锁需要基地等级
        /// </summary>
        [Column("unlock_level")]
        public StarBaseLevel UnlockLevel { get; set; }

        public StarBaseModule Clone()
        {
            return (StarBaseModule)this.MemberwiseClone();
        }
    }
}
