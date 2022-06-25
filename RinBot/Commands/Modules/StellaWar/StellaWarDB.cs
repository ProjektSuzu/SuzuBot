using RinBot.Commands.Modules.Arcaea;
using RinBot.Commands.Modules.StellaWar.Core.Building;
using RinBot.Commands.Modules.StellaWar.Core.Ship;
using RinBot.Utils.Database;
using RinBot.Utils.Database.Tables;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Commands.Modules.StellaWar
{
    internal class StellaWarDB
    {
        private static readonly string stellaWarDbPath = Path.Combine(DatabaseManager.DbPath, "stellaWar.db");
        public SQLiteConnection dbConnection = new(stellaWarDbPath);

        #region 单例模式
        private static StellaWarDB instance;
        public static StellaWarDB Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new StellaWarDB();
                }
                return instance;
            }
        }
        private StellaWarDB()
        {
            dbConnection.CreateTable<StarBaseInfo>();
            dbConnection.CreateTable<BaseShip>();
            dbConnection.CreateTable<StarBaseModule>();
        }
        #endregion
    }

    [Table("T_STELLA_WAR_BASE_INFO")]
    internal class StarBaseInfo
    {
        [PrimaryKey]
        [Column("owner")]
        public uint Owner { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("level")]
        public StarBaseLevel Level { get; set; }

        [Column("under_attack")]
        public bool UnderAttack { get; set; }

        [Column("modules")]
        public string Modules { get; set; }

        [Column("starbase_build_sequence")]
        public string StarBaseBuildSequence { get; set; }

        [Column("ship_build_sequence")]
        public string ShipBuildSequence { get; set; }

        [Column("starbase_repair_sequence")]
        public string ShipRepairSequence { get; set; }

        [Column("all_ship")]
        public string AllShip { get; set; }
    }
}
