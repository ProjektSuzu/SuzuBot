using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Commands.Modules.StellaWar.Core.Ship
{
    [Table("T_SHIP_TECH")]
    internal class BaseShip
    {
        /// <summary>
        /// 舰船代码
        /// </summary>
        [Column("code")]
        public string Code { get; set; }
        /// <summary>
        /// 舰船名称
        /// </summary>
        [Column("name")]
        public string Name { get; set; }
        /// <summary>
        /// 舰船描述
        /// </summary>
        [Column("description")]
        public string Description { get; set; }
        /// <summary>
        /// 舰船船体值
        /// </summary>
        [Column("hp")]
        public int Health { get; set; }
        /// <summary>
        /// 最大舰船船体值
        /// </summary>
        [Column("max_hp")]
        public int MaxHealth { get; set; }
        /// <summary>
        /// 舰船护盾值
        /// </summary>
        [Column("shield")]
        public int Shield { get; set; }
        /// <summary>
        /// 最大舰船护盾值
        /// </summary>
        [Column("max_shield")]
        public int MaxShield { get; set; }
        /// <summary>
        /// 最低舰船攻击力
        /// </summary>
        [Column("min_attack")]
        public int MinAttack { get; set; }
        /// <summary>
        /// 最高舰船攻击力
        /// </summary>
        [Column("max_attack")]
        public int MaxAttack { get; set; }
        /// <summary>
        /// 舰船命中率 百分比
        /// </summary>
        [Column("accuracy")]
        public float Accuracy { get; set; }
        /// <summary>
        /// 舰船闪避率 百分比
        /// </summary>
        [Column("evasion")]
        public float Evasion { get; set; }
        /// <summary>
        /// 舰船索敌率 百分比
        /// </summary>
        [Column("tracking")]
        public float Tracking { get; set; }

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
   
        public BaseShip Clone()
        {
            return (BaseShip)this.MemberwiseClone();
        }
    }
}
