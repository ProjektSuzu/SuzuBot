using Newtonsoft.Json;
using RinBot.Commands.Modules.StellaWar.Core.Ship;
using RinBot.Utils.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Commands.Modules.StellaWar.Core.Building
{
    public enum StarBaseLevel
    {
        Outpost,
        Starport,
        Starhold,
        StarFortress,
        Citadel
    }

    public enum ShipBuildResult
    {
        OK,
        InsufficientFunds,
        ShipCapacityFull,
        ShipTechLocked,
        ShipNotExist
    }

    public enum ModuleBuildResult
    {
        OK,
        InsufficientFunds,
        ModuleCapacityFull,
        ModuleTechLocked,
        SingletonModule,
        ModuleNotExist
    }

    internal class StarBase
    {
        public uint Owner;
        public string Name;

        public StarBaseLevel Level = StarBaseLevel.Outpost;

        public int MaxShipCapacity
        {
            get
            {
                int result = 0;
                switch (Level)
                {
                    case StarBaseLevel.Outpost:
                        result = 8;
                        break;
                    case StarBaseLevel.Starport:
                        result = 16;
                        break;
                    case StarBaseLevel.Starhold:
                        result = 32;
                        break;
                    case StarBaseLevel.StarFortress:
                        result = 64;
                        break;
                    case StarBaseLevel.Citadel:
                        result = 128;
                        break;
                }

                //每个锚地额外提供 8 个容纳量
                result += Modules.Where(x => x.ID == "anchorage").Count() * 8;

                return result;
            }
        }
        public int MaxModuleCapacity
        {
            get
            {
                switch (Level)
                {
                    case StarBaseLevel.Outpost:
                        return 2;
                    case StarBaseLevel.Starport:
                        return 4;
                    case StarBaseLevel.Starhold:
                        return 8;
                    case StarBaseLevel.StarFortress:
                        return 16;
                    case StarBaseLevel.Citadel:
                        return 32;
                }
                return 0;
            }
        }
        public int DefensiveGunGroupAttack
        {
            get
            {
                switch (Level)
                {
                    case StarBaseLevel.Outpost:
                        return 10;
                    case StarBaseLevel.Starport:
                        return 20;
                    case StarBaseLevel.Starhold:
                        return 40;
                    case StarBaseLevel.StarFortress:
                        return 80;
                    case StarBaseLevel.Citadel:
                        return 160;
                }
                return 0;
            }
        }

        public bool UnderAttack = false;
        

        public DateTime EmergencyShield = DateTime.Now;

        public List<StarBaseModule> Modules = new();

        public List<StarBaseModule> StarBaseBuildSequence = new();
        public List<BaseShip> ShipBuildSequence = new();
        public List<BaseShip> ShipRepairSequence = new();

        public List<BaseShip> AllShip = new();
        public List<BaseShip> ShipInBase = new();

        public StarBase(uint owner, string name)
        {
            this.Owner = owner;
            this.Name = name;
        }

        public StarBase(StarBaseInfo info)
        {
            this.Owner = info.Owner;
            this.Name = info.Name;
            this.Level = info.Level;
            this.UnderAttack = info.UnderAttack;

            this.Modules = JsonConvert.DeserializeObject<List<StarBaseModule>>(info.Modules) ?? new();
            this.StarBaseBuildSequence = JsonConvert.DeserializeObject<List<StarBaseModule>>(info.StarBaseBuildSequence) ?? new();

            this.ShipBuildSequence = JsonConvert.DeserializeObject<List<BaseShip>>(info.ShipBuildSequence) ?? new();

            this.AllShip = JsonConvert.DeserializeObject<List<BaseShip>>(info.AllShip) ?? new();
        }

        public StarBaseInfo Save()
        {
            StarBaseInfo info = new();
            info.Owner = Owner;
            info.Name = Name;
            info.Level = Level;
            info.UnderAttack = UnderAttack;

            info.Modules = JsonConvert.SerializeObject(Modules);
            info.StarBaseBuildSequence = JsonConvert.SerializeObject(StarBaseBuildSequence);
            info.ShipBuildSequence = JsonConvert.SerializeObject(ShipBuildSequence);

            info.AllShip = JsonConvert.SerializeObject(AllShip);
            return info;
        }

        public ShipBuildResult BuildShip(string shipCode, uint num = 1)
        {
            if (ShipBuildSequence.Count + AllShip.Count + num > MaxShipCapacity)
                return ShipBuildResult.ShipCapacityFull;
            var blueprint = StellaWarDB.Instance.dbConnection.Table<BaseShip>().Where(x => x.UnlockLevel <= Level).FirstOrDefault(x => x.Code == shipCode || x.Name == shipCode);
            if (blueprint == null)
            {
                blueprint = StellaWarDB.Instance.dbConnection.Table<BaseShip>().Where(x => x.Code == shipCode || x.Name == shipCode).ToList().First() ?? null;
                if (blueprint == null)
                    return ShipBuildResult.ShipNotExist;
                else
                    return ShipBuildResult.ShipTechLocked;
            }

            long cost = (long)blueprint.BuildCostKB * num;
            var info = UserInfoManager.GetUserInfo(Owner);
            if (info.coin <= cost)
                return ShipBuildResult.InsufficientFunds;

            for (uint i = num; i > 0; i--)
            {
                ShipBuildSequence.Add(blueprint.Clone());
            }
            return ShipBuildResult.OK;
        }

        public ModuleBuildResult BuildModule(string moduleID)
        {
            if (StarBaseBuildSequence.Count + Modules.Count > MaxModuleCapacity)
                return ModuleBuildResult.ModuleCapacityFull;
            var blueprint = StellaWarDB.Instance.dbConnection.Table<StarBaseModule>().Where(x => x.UnlockLevel <= Level).FirstOrDefault(x => x.ID == moduleID || x.Name == moduleID);
            if (blueprint == null)
            {
                blueprint = StellaWarDB.Instance.dbConnection.Table<StarBaseModule>().FirstOrDefault(x => x.ID == moduleID || x.Name == moduleID) ?? null;
                if (blueprint == null)
                    return ModuleBuildResult.ModuleNotExist;
                else
                    return ModuleBuildResult.ModuleTechLocked;
            }

            if (StarBaseBuildSequence.Any(x => x.ID == moduleID) || Modules.Any(x => x.ID == moduleID))
                return ModuleBuildResult.SingletonModule;

            long cost = (long)blueprint.BuildCostKB;
            var info = UserInfoManager.GetUserInfo(Owner);
            if (info.coin <= cost)
                return ModuleBuildResult.InsufficientFunds;

            StarBaseBuildSequence.Add(blueprint.Clone());
            return ModuleBuildResult.OK; 
        }

        public void Flush()
        {
            //移除在战斗中被击毁的舰船
            AllShip.RemoveAll(x => x.Health <= 0);
            ShipRepairSequence.RemoveAll(x => x.Health <= 0);

            //添加舰船到修复队列
            ShipRepairSequence = ShipRepairSequence.Union(AllShip.Where(x => x.Health < x.MaxHealth)).ToList();

            //刷新舰船建造队列
            ShipBuildSequence.Where(x => x.BuildTimeMinute <= 0).ToList().ForEach(x => AllShip.Add(x));
            ShipBuildSequence.RemoveAll(x => x.BuildTimeMinute <= 0);
        }

        public void Simulate()
        {
            //移除在战斗中被击毁的舰船
            AllShip.RemoveAll(x => x.Health <= 0);

            long memoryChange = 0L;

            //被攻击了就不能进行大部分操作
            if (!UnderAttack)
            {
                int engineerCount = AllShip.Where(x => x.Code == "engineer").Count();
                int repairModuleCount = Modules.Where(x => x.ID == "repair_station").Count();
                int dockModuleCount = Modules.Where(x => x.ID == "dock").Count();

                //舰船回盾
                foreach (var ship in AllShip.Where(x => x.Shield < x.MaxShield))
                {
                    ship.Shield = ship.MaxShield;
                }

                //将受损的舰船加入到维修队列中
                //每分钟回复最大生命值的 1% * (基地船坞个数 + 基地维修站个数 * 4 + 工程船个数)
                ShipRepairSequence = ShipRepairSequence.Union(AllShip.Where(x => x.Health < x.MaxHealth)).ToList();
                
                if (ShipRepairSequence.Count > 0)
                {
                    foreach (var ship in ShipRepairSequence)
                    {
                        if (ship.Health < ship.MaxHealth)
                        {
                            ship.Health += (int)Math.Ceiling(ship.MaxHealth * 0.01f
                                * (4 * repairModuleCount + engineerCount + dockModuleCount));
                            if (ship.Health > ship.MaxHealth)
                                ship.Health = ship.MaxHealth;
                        }
                    }

                    ShipRepairSequence.RemoveAll(x => x.Health == x.MaxHealth);
                }

                //开始建造新的舰船
                //最大同时建造数量为 1 + 基地船坞个数 + 工程船个数 / 4
                if (ShipBuildSequence.Count > 0)
                {
                    var buildList = ShipBuildSequence.Take(1 + dockModuleCount + engineerCount / 4).ToList();
                    foreach (var ship in buildList)
                    {
                        ship.BuildTimeMinute--;
                    }

                    ShipBuildSequence.RemoveAll(x => x.BuildTimeMinute <= 0);
                    foreach (var ship in buildList.Where(x => x.BuildTimeMinute <= 0))
                    {
                        AllShip.Add(ship);
                    }
                }

                //开始修建基地模块
                //一次只能建造一个模块 但是工程船可以加速这一过程
                if (StarBaseBuildSequence.Count > 0)
                {
                    var module = StarBaseBuildSequence.First();
                    module.BuildTimeMinute -= (1 + engineerCount);

                    if (module.BuildTimeMinute <= 0)
                    {
                        StarBaseBuildSequence.Remove(module);
                        Modules.Add(module);
                    }
                }

                //工程船采集内存 基础值为 200 KB
                int collect = 200;
                if (Modules.Any(x => x.ID == "memory_refinery"))
                    collect = (int)Math.Round(collect * 0.2f);
                memoryChange += collect * engineerCount;
            }

            //模块消耗内存
            foreach (var module in Modules)
            {
                memoryChange -= module.MaintainCostKB;
            }
        }

        public long CalcMemoryBalance()
        {
            long memoryChange = 0L;
            int engineerCount = AllShip.Where(x => x.Code == "engineer").Count();
            if (!UnderAttack)
            {
                //工程船采集内存 基础值为 200 KB
                int collect = 200;
                if (Modules.Any(x => x.ID == "memory_refinery"))
                    collect = (int)Math.Round(collect * 0.2f);
                memoryChange += collect * engineerCount;
            }
            //模块消耗内存
            foreach (var module in Modules)
            {
                memoryChange -= module.MaintainCostKB;
            }
            return memoryChange;
        }

        public DateTime CalcRepairCompleteDate()
        {
            var date = DateTime.Now;
            int engineerCount = AllShip.Where(x => x.Code == "engineer").Count();
            int repairModuleCount = Modules.Where(x => x.ID == "repair_station").Count();
            int dockModuleCount = Modules.Where(x => x.ID == "dock").Count();

            var ship = ShipRepairSequence.OrderBy(x => (float)x.Health / x.MaxHealth).First();
            date.AddMinutes(Math.Round((float)ship.Health / (0.01f * ship.MaxHealth * (4 * repairModuleCount + engineerCount + dockModuleCount + 1))));
            return date;
        }
    }
}
