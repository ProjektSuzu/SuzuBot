using RinBot.Commands.Modules.StellaWar.Core.Building;
using RinBot.Commands.Modules.StellaWar.Core.War;
using RinBot.Core.Attributes.Command.Modules;
using RinBot.Core.Attributes.CommandSet;
using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using RinBot.Core.Attributes.Command.Modules;
using RinBot.Core.Attributes.CommandSet;
using RinBot.Commands.Modules.StellaWar.Core.Ship;
using System.Text;
using RinBot.Utils;
using Konata.Core.Message.Model;
using RinBot.Utils.Database.Tables;
using NLog;
using RinBot.Core.Components;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

namespace RinBot.Commands.Modules.StellaWar
{
    [CommandSet("群星争霸", "com.akulak.stellaWar")]
    internal class StellaWar : BaseCommand
    {
        private static readonly string TAG = "STELLA";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        Timer dayLoopTimer;

        List<StarBase> starBases = new();

        public override void OnInit()
        {
            foreach (var info in StellaWarDB.Instance.dbConnection.Table<StarBaseInfo>().ToList())
            {
                starBases.Add(new StarBase(info));
            }
            starBases.ForEach(x => x.Flush());
            dayLoopTimer = new Timer(new TimerCallback(Loop));
            dayLoopTimer.Change((60 - DateTime.Now.Second) * 1000, 60000);
        }

        private void Loop(object value)
        {
            //Parallel.ForEach(battles, battles => battles.Simulate());
            if (starBases.Count > 0)
                Parallel.ForEach(starBases, starBase => { starBase.Simulate(); });
            SaveAll();
        }

        private void SaveAll()
        {
            List<StarBaseInfo> starBaseInfos = new();
            starBases.ForEach(x => starBaseInfos.Add(x.Save()));
            foreach (var info in starBaseInfos)
            {
                if (StellaWarDB.Instance.dbConnection.Table<StarBaseInfo>().Where(x => x.Owner == info.Owner).Count() <= 0)
                {
                    StellaWarDB.Instance.dbConnection.Insert(info);
                }
            }
            Logger.Info($"{StellaWarDB.Instance.dbConnection.UpdateAll(starBaseInfos)} Data(s) Saved.");
        }

        [GroupMessageCommand("StellaWar", new[] { @"^stella_war\s?([\s\S]+)?", @"^stellaris\s?([\s\S]+)?" })]
        public void OnStellaWar(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? funcName = args.FirstOrDefault();
            string reply = "";

            if (funcName == null)
            {
                OnBaseInfo(bot, messageEvent);
                return;
            }

            args = args.Skip(1).ToList();
            switch (funcName)
            {
                case "attack":
                    OnAttack(bot, messageEvent, args);
                    return;

                case "starbase":
                    OnBaseInfo(bot, messageEvent);
                    return;

                case "ship-info":
                    OnShipInfo(bot, messageEvent, args);
                    return;

                case "build-ship":
                    OnBuildShip(bot, messageEvent, args);
                    return;

                case "module-info":
                    OnModuleInfo(bot, messageEvent, args);
                    return;

                case "build-module":
                    OnBuildModule(bot, messageEvent, args);
                    return;

                case "cancel-build-ship":
                    OnCancelShipBuild(bot, messageEvent, args);
                    return;

                case "recycle-ship":
                    OnRecycleShip(bot, messageEvent, args);
                    return;

                case "base-rename":
                    OnBaseRename(bot, messageEvent, args);
                    return;

                case "base-upgrade":
                    OnBaseUpgrade(bot, messageEvent);
                    return;

                case "simulate":
                    OnSimulate(bot, messageEvent);
                    return;

                case "instant-build":
                    OnInstantBuild(bot, messageEvent);
                    return;

                case "give-ship":
                    OnGiveShip(bot, messageEvent, args);
                    return;

                default:
                    {
                        reply = $"错误: 找不到功能: \"{funcName}\"";
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                        return;
                    }
            }
        }

        public void OnBaseInfo(Bot bot, GroupMessageEvent messageEvent)
        {
            var starbase = starBases.FirstOrDefault(x => x.Owner == messageEvent.MemberUin);
            var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);
            if (starbase == null)
            {
                starbase = new StarBase(messageEvent.MemberUin, $"{messageEvent.MemberCard} 帝国");
                starBases.Add(starbase);
            }
            StringBuilder sb = new();
            starbase.Flush();
            int engineerCount = starbase.AllShip.Where(x => x.Code == "engineer").Count();
            int dockModuleCount = starbase.Modules.Where(x => x.ID == "dock").Count();

            sb.AppendLine("[群星争霸]基地信息");
            sb.AppendLine($"{starbase.Name}");
            sb.AppendLine($"基地等级: {starbase.Level}");
            sb.AppendLine($"内存储量: {UserInfoManager.CoinToString(info.coin)}");
            sb.AppendLine($"内存收支: {UserInfoManager.CoinToString(starbase.CalcMemoryBalance())}");
            sb.AppendLine($"模块容量: {starbase.Modules.Count()}/{starbase.MaxModuleCapacity}");
            if (starbase.Modules.Count > 0)
            {
                foreach (var module in starbase.Modules)
                {
                    sb.AppendLine($"--{module.Name}");
                }
            }
            sb.AppendLine($"模块建造队列: {starbase.StarBaseBuildSequence.Count}");
            if (starbase.StarBaseBuildSequence.Count > 0)
            {
                List<StarBaseModule> temp = new();
                foreach (var module in starbase.StarBaseBuildSequence)
                {
                    temp.Add(module);
                }

                var first = temp.First();
                temp = temp.Skip(1).ToList();

                sb.AppendLine($"--{first.Name}  ETA: {first.BuildTimeMinute / (1 + engineerCount)} min");
                foreach (var module in temp)
                {
                    sb.AppendLine($"--{module.Name}  WAITING");
                }
            }
            sb.AppendLine();

            sb.AppendLine($"舰船容量: {starbase.AllShip.Count()}/{starbase.MaxShipCapacity}");
            if (starbase.AllShip.Count > 0)
            {
                List<BaseShip> temp = new();
                foreach (var ship in starbase.AllShip)
                {
                    temp.Add(ship);
                }

                while (temp.Count > 0)
                {
                    var ship = temp.First();
                    var count = temp.RemoveAll(x => x.Code == ship.Code);

                    sb.AppendLine($"--{ship.Name}: {count}");
                }
                sb.AppendLine(); 
            }

            sb.AppendLine($"维修队列: {starbase.ShipRepairSequence.Count}");
            if (starbase.ShipRepairSequence.Count > 0)
            {
                List<BaseShip> temp = new();
                foreach (var ship in starbase.AllShip)
                {
                    temp.Add(ship);
                }

                while (temp.Count > 0)
                {
                    var ship = temp.First();
                    var count = temp.RemoveAll(x => x.Code == ship.Code);

                    sb.AppendLine($"--{ship.Name}: {count}");
                }
                sb.AppendLine();
                sb.AppendLine($"预计在 {starbase.CalcRepairCompleteDate():g} 全部维修完成");
                sb.AppendLine();
            }

            sb.AppendLine($"舰船建造队列: {starbase.ShipBuildSequence.Count}");
            if (starbase.ShipBuildSequence.Count > 0)
            {
                var buildList = starbase.ShipBuildSequence.Take(1 + dockModuleCount + engineerCount / 4).ToList();
                var waitList = starbase.ShipBuildSequence.Skip(1 + dockModuleCount + engineerCount / 4).ToList();
                foreach (var ship in buildList)
                {
                    sb.AppendLine($"--{ship.Name}  ETA: {ship.BuildTimeMinute} min");
                }
                foreach (var ship in waitList)
                {
                    sb.AppendLine($"--{ship.Name}  WAITING");
                }
                sb.AppendLine();
            }

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text(sb.ToString()));
            return;
        }

        public void OnBuildShip(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var num = 1u;
            var code = "";

            if (args.Count <= 0)
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text("错误: 缺少参数: <shipCode>"));
                return;
            }
            else
            {
                code = args[0];
                if (args.Count > 1)
                {
                    if (!uint.TryParse(args[1], out num) || num < 1)
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 参数错误: {args[0]} => [<num>]."));
                        return;
                    }
                }
            }
            var starbase = starBases.FirstOrDefault(x => x.Owner == messageEvent.MemberUin);
            if (starbase == null)
            {
                starbase = new StarBase(messageEvent.MemberUin, $"{messageEvent.MemberCard} 的基地");
                starBases.Add(starbase);
            }

            var ship = StellaWarDB.Instance.dbConnection.Table<BaseShip>().Where(x => x.Code == code || x.Name == code).ToList().First() ?? null;
            var result = starbase.BuildShip(code, num);
            switch (result)
            {
                case ShipBuildResult.OK:
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"{num} 艘舰船已加入建造队列"));
                        return;
                    }

                case ShipBuildResult.InsufficientFunds:
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 内存不足\n需要 {UserInfoManager.CoinToString((long)ship.BuildCostKB * num)}"));
                        return;
                    }

                case ShipBuildResult.ShipCapacityFull:
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 基地舰船容量已满"));
                        return;
                    }

                case ShipBuildResult.ShipTechLocked:
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 尚未解锁关于 {ship.Name} 的科技"));
                        return;
                    }

                case ShipBuildResult.ShipNotExist:
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 舰船 \"{code}\" 不存在"));
                        return;
                    }
            }
        }

        public void OnBuildModule(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var id = "";

            if (args.Count <= 0)
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text("错误: 缺少参数: <moduleID>"));
                return;
            }
            else
            {
                id = args[0];
            }
            var starbase = starBases.FirstOrDefault(x => x.Owner == messageEvent.MemberUin);
            if (starbase == null)
            {
                starbase = new StarBase(messageEvent.MemberUin, $"{messageEvent.MemberCard} 的基地");
                starBases.Add(starbase);
            }

            var module = StellaWarDB.Instance.dbConnection.Table<StarBaseModule>().Where(x => x.ID == id || x.Name == id).ToList().First() ?? null;
            var result = starbase.BuildModule(id);
            switch (result)
            {
                case ModuleBuildResult.OK:
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"{module.Name} 已加入建造队列"));
                        return;
                    }

                case ModuleBuildResult.SingletonModule:
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"基地只能建造一个 {module.Name}"));
                        return;
                    }

                case ModuleBuildResult.InsufficientFunds:
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 内存不足\n需要 {UserInfoManager.CoinToString((long)module.BuildCostKB)}"));
                        return;
                    }

                case ModuleBuildResult.ModuleCapacityFull:
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 基地模块容量已满"));
                        return;
                    }

                case ModuleBuildResult.ModuleTechLocked:
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 尚未解锁关于 {module.Name} 的科技"));
                        return;
                    }

                case ModuleBuildResult.ModuleNotExist:
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 模块 \"{id}\" 不存在"));
                        return;
                    }
            }
        }

        public void OnAttack(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var uin = messageEvent.MemberUin;
            var target = 0u;
            var targetName = "";

            var starbase = starBases.First(x => x.Owner == uin);
            var info = UserInfoManager.GetUserInfo(uin);
            StarBase targetBase;
            if (args.Count <= 0)
            {
                messageEvent.Reply(bot, new MessageBuilder()
                                .Add(ReplyChain.Create(messageEvent.Message))
                                .Text($"错误: 未指定要攻击的目标."));
                return;
            }
            else
            {
                if (!uint.TryParse(args[0], out target))
                {
                    AtChain? atChain = (AtChain?)messageEvent.Message.Chain.FirstOrDefault(x => x is AtChain);
                    if (atChain == null)
                    {
                        messageEvent.Reply(bot, new MessageBuilder()
                                .Add(ReplyChain.Create(messageEvent.Message))
                                .Text($"错误: 参数错误: {args[0]} => <targetUin>."));
                        return;
                    }
                    target = atChain.AtUin;
                    targetName = bot.GetGroupMemberList(messageEvent.GroupUin).Result.First(x => x.Uin == target).NickName;
                }

                if (target == uin)
                {
                    messageEvent.Reply(bot, new MessageBuilder()
                                .Add(ReplyChain.Create(messageEvent.Message))
                                .Text($"错误：我们不能攻击自己"));
                    return;
                }

                targetBase = starBases.FirstOrDefault(x => x.Owner == target);
                if (targetBase == null)
                {
                    messageEvent.Reply(bot, new MessageBuilder()
                                .Add(ReplyChain.Create(messageEvent.Message))
                                .Text($"错误：攻击目标不存在"));
                    return;
                }

                if (targetBase.EmergencyShield > DateTime.Now)
                {
                    messageEvent.Reply(bot, new MessageBuilder()
                                .Add(ReplyChain.Create(messageEvent.Message))
                                .Text($"错误：目标基地已开启屏障 无法进攻"));
                    return;
                }
            }
            var targetInfo = UserInfoManager.GetUserInfo(target);
            targetName = bot.GetGroupMemberList(messageEvent.GroupUin).Result.First(x => x.Uin == target).NickName;

            starbase.EmergencyShield = DateTime.Now;
            AggressiveWar war = new(messageEvent.MemberUin, target, starbase, targetBase);
            
            StringBuilder sb = new();
            sb.AppendLine("[群星争霸]侵略战争");
            sb.AppendLine($"{starbase.Name} 对 {targetBase.Name}");
            sb.AppendLine();

            sb.AppendLine("进攻方舰队");
            List<BaseShip> temp = new();
            foreach (var ship in war.AttackerFleet)
            {
                temp.Add(ship);
            }

            while (temp.Count > 0)
            {
                var ship = temp.First();
                var count = temp.RemoveAll(x => x.Code == ship.Code);

                sb.Append($"{ship.Name}: {count}");
            }
            sb.AppendLine();

            sb.AppendLine("防守方舰队");
            temp = new();
            foreach (var ship in war.DefenderFleet)
            {
                temp.Add(ship);
            }

            while (temp.Count > 0)
            {
                var ship = temp.First();
                var count = temp.RemoveAll(x => x.Code == ship.Code);

                sb.Append($"{ship.Name}: {count}");
            }
            sb.AppendLine();

            while (!war.IsOver)
                war.Simulate();

            sb.AppendLine($"战争持续了 {war.DayPassed} 天");

            if (war.IsSuccess)
            {
                if (targetInfo.coin <= war.MemoryRobbed)
                    war.MemoryRobbed = targetInfo.coin;
                targetInfo.coin -= war.MemoryRobbed;
                info.coin += war.MemoryRobbed;

                UserInfoManager.UpdateUserInfo(targetInfo);
                UserInfoManager.UpdateUserInfo(info);

                sb.AppendLine($"进攻方成功达成战争目标 并且掠夺了对方 {UserInfoManager.CoinToString(war.MemoryRobbed)}");
            }
            else
            {
                sb.AppendLine($"进攻方未能达成战争目标");
            }
            sb.AppendLine();

            sb.AppendLine("[战报总结]");
            sb.AppendLine();
            sb.AppendLine($"进攻方紧急逃脱舰船数: {war.AttackerRetreat.Count}");
            if (war.AttackerRetreat.Count > 0)
            {
                temp = new();
                foreach (var ship in war.AttackerRetreat)
                {
                    temp.Add(ship);
                }

                while (temp.Count > 0)
                {
                    var ship = temp.First();
                    var count = temp.RemoveAll(x => x.Code == ship.Code);

                    sb.AppendLine($"{ship.Name}: {count}");
                }
            }
            sb.AppendLine($"进攻方被击毁舰船数: {war.AttackerLost.Count}");
            if (war.AttackerLost.Count > 0)
            {
                temp = new();
                foreach (var ship in war.AttackerLost)
                {
                    temp.Add(ship);
                }

                while (temp.Count > 0)
                {
                    var ship = temp.First();
                    var count = temp.RemoveAll(x => x.Code == ship.Code);

                    sb.AppendLine($"{ship.Name}: {count}");
                }
            }

            sb.AppendLine($"防守方紧急逃脱舰船数: {war.DefenderRetreat.Count}");
            if (war.DefenderRetreat.Count > 0)
            {
                temp = new();
                foreach (var ship in war.DefenderRetreat)
                {
                    temp.Add(ship);
                }

                while (temp.Count > 0)
                {
                    var ship = temp.First();
                    var count = temp.RemoveAll(x => x.Code == ship.Code);

                    sb.AppendLine($"{ship.Name}: {count}");
                }
            }
            sb.AppendLine($"防守方被击毁舰船数: {war.DefenderLost.Count}");
            if (war.DefenderLost.Count > 0)
            {
                temp = new();
                foreach (var ship in war.DefenderLost)
                {
                    temp.Add(ship);
                }

                while (temp.Count > 0)
                {
                    var ship = temp.First();
                    var count = temp.RemoveAll(x => x.Code == ship.Code);

                    sb.AppendLine($"{ship.Name}: {count}");
                }
            }
            sb.AppendLine();
            SaveAll();

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text(sb.ToString()));
            return;

        }

        public void OnShipInfo(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            StringBuilder sb = new();
            var shipcode = "";
            if (args.Count <= 0)
            {
                foreach (var ship in StellaWarDB.Instance.dbConnection.Table<BaseShip>().ToList())
                {
                    sb.AppendLine($"{ship.Code}: {ship.Name}");
                }
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text(sb.ToString()));
                return;
            }
            else
            {
                shipcode = args[0];
                BaseShip ship;
                ship = StellaWarDB.Instance.dbConnection.Table<BaseShip>().Where(x => x.Code == shipcode || x.Name == shipcode).FirstOrDefault();
                if (ship == null)
                {
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 舰船 \"{shipcode}\" 不存在"));
                    return;
                }
                else
                {
                    sb.AppendLine($"舰船名称: {ship.Name}");
                    sb.AppendLine($"舰船代码: {ship.Code}");
                    sb.AppendLine($"解锁等级: {ship.UnlockLevel}");
                    sb.AppendLine($"最大船体值: {ship.MaxHealth}");
                    sb.AppendLine($"最大护盾值: {ship.MaxShield}");
                    sb.AppendLine($"攻击: {ship.MinAttack}~{ship.MaxAttack}");
                    sb.AppendLine($"命中: {ship.Accuracy * 100:0}%");
                    sb.AppendLine($"闪避: {ship.Evasion * 100:0}%");
                    sb.AppendLine($"索敌: {ship.Tracking * 100:0}%");
                    sb.AppendLine($"造价: {UserInfoManager.CoinToString(ship.BuildCostKB)}");
                    sb.AppendLine($"耗时: {ship.BuildTimeMinute} 分钟");
                    sb.AppendLine();
                    sb.AppendLine(ship.Description);

                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text(sb.ToString()));
                }
            }
        }

        public void OnModuleInfo(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            StringBuilder sb = new();
            var id = "";
            if (args.Count <= 0)
            {
                foreach (var module in StellaWarDB.Instance.dbConnection.Table<StarBaseModule>().ToList())
                {
                    sb.AppendLine($"{module.ID}: {module.Name}");
                }
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text(sb.ToString()));
                return;
            }
            else
            {
                id = args[0];
                StarBaseModule module;
                module = StellaWarDB.Instance.dbConnection.Table<StarBaseModule>().Where(x => x.ID == id || x.Name == id).FirstOrDefault();
                if (module == null)
                {
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 模块 \"{id}\" 不存在"));
                    return;
                }
                else
                {
                    sb.AppendLine($"模块名称: {module.Name}");
                    sb.AppendLine($"模块代码: {module.ID}");
                    sb.AppendLine($"解锁等级: {module.UnlockLevel}");
                    sb.AppendLine($"维护耗费: {UserInfoManager.CoinToString(module.MaintainCostKB)}");
                    sb.AppendLine($"造价: {UserInfoManager.CoinToString(module.BuildCostKB)}");
                    sb.AppendLine($"耗时: {module.BuildTimeMinute} 分钟");
                    sb.AppendLine();
                    sb.AppendLine(module.Description);

                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text(sb.ToString()));
                }
            }
        }

        public void OnBaseUpgrade(Bot bot, GroupMessageEvent messageEvent)
        {
            var starbase = starBases.FirstOrDefault(x => x.Owner == messageEvent.MemberUin);
            var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);

            if (starbase.Level == StarBaseLevel.Citadel)
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text("已升至最高级"));
                return;
            }

            if (starbase.Modules.Count < starbase.MaxModuleCapacity)
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text("只有模块容量为满时才能升级基地"));
                return;
            }

            long upgradeCost = 0L;
            switch (starbase.Level)
            {
                case StarBaseLevel.Outpost:
                    upgradeCost = 500000L;
                    break;

                case StarBaseLevel.Starport:
                    upgradeCost = 1000000L;
                    break;

                case StarBaseLevel.Starhold:
                    upgradeCost = 5000000L;
                    break;

                case StarBaseLevel.StarFortress:
                    upgradeCost = 10000000L;
                    break;

                case StarBaseLevel.Citadel:
                    upgradeCost = 50000000L;
                    break;
            }

            if (info.coin < upgradeCost)
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"你没有足够的内存\n升级到 {starbase.Level + 1} 需要 {UserInfoManager.CoinToString(upgradeCost)}\n" +
                        $"而你只有 {UserInfoManager.CoinToString(info.coin)}"));
                return;
            }
            else
            {
                info.coin -= upgradeCost;
                starbase.Level++;
                UserInfoManager.UpdateUserInfo(info);
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"{starbase.Name} 已升级至 {starbase.Level}"));
                return;
            }
        }

        public void OnRecycleShip(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var num = 1u;
            var code = "";

            if (args.Count <= 0)
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text("错误: 缺少参数: <shipCode>"));
                return;
            }
            else
            {
                code = args[0];
                if (args.Count > 1)
                {
                    if (!uint.TryParse(args[1], out num) || num < 1)
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 参数错误: {args[0]} => [<num>]."));
                        return;
                    }
                }
            }
            var starbase = starBases.FirstOrDefault(x => x.Owner == messageEvent.MemberUin);
            if (starbase == null)
            {
                starbase = new StarBase(messageEvent.MemberUin, $"{messageEvent.MemberCard} 的基地");
                starBases.Add(starbase);
            }

            var ship = StellaWarDB.Instance.dbConnection.Table<BaseShip>().Where(x => x.Code == code || x.Name == code).ToList().First() ?? null;
            if (ship == null)
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 舰船 \"{code}\" 不存在"));
                return;
            }
            if (starbase.AllShip.Where(x => x.Code == ship.Code).Count() < num)
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 基地中并不含有 {num} 艘 {ship.Name}"));
                return;
            }
            else
            {
                var recycleList = starbase.AllShip.Where(x => x.Code == ship.Code).OrderBy(x => x.Health).Take((int)num).ToList();
                recycleList.ForEach(x => { starbase.ShipRepairSequence.Remove(x); starbase.AllShip.Remove(x); });
                var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);
                var recycleMemory = 0L;
                recycleList.ForEach(x => recycleMemory += (int)(x.BuildCostKB * 0.1f));
                info.coin += recycleMemory;
                UserInfoManager.UpdateUserInfo(info);

                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"已回收 {num} 艘 {ship.Name}\n回收内存 {UserInfoManager.CoinToString(recycleMemory)}"));
                return;
            }
        }

        public void OnCancelShipBuild(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var num = 1;
            var starbase = starBases.First(x => x.Owner == messageEvent.MemberUin);
            var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);

            if (args.Count > 0)
            {
                if (!int.TryParse(args[0], out num) || num < 1)
                {
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text($"错误: 参数错误: {args[0]} => [<num>]."));
                    return;
                }
            }

            if (num > starbase.ShipBuildSequence.Count)
                num = starbase.ShipBuildSequence.Count;

            var cancelList = starbase.ShipBuildSequence.Reverse<BaseShip>().ToList().Take(num);
            starbase.ShipBuildSequence = starbase.ShipBuildSequence.Take(starbase.ShipBuildSequence.Count - num).ToList();

            foreach (var ship in cancelList)
            {
                info.coin += ship.BuildCostKB;
            }

            UserInfoManager.UpdateUserInfo(info);
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text($"已取消了 {num} 个建造队列."));
            return;
        }

        public void OnBaseRename(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var starbase = starBases.First(x => x.Owner == messageEvent.MemberUin);
            starbase.Name = String.Join(' ', args);
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text($"已将基地更名为 {String.Join(' ', args)}"));
            return;
        }

        public void OnSimulate(Bot bot, GroupMessageEvent messageEvent)
        {
            if (PermissionManager.Instance.GetPermission(bot, messageEvent.GroupUin, messageEvent.MemberUin) < Permission.Admin)
            {
                messageEvent.Reply(bot, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text(
                        $"你没有权限使用此命令\n" +
                        $"此命令需要的权限等级为 {Permission.Admin}\n" +
                        $"你的权限等级为 {PermissionManager.Instance.GetPermission(bot, messageEvent.GroupUin, messageEvent.MemberUin)}"));
                return;
            }

            var before = DateTime.Now;
            Loop(null);
            var after = DateTime.Now;
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"Simulate Completed.({(after - before).TotalMilliseconds}ms)"));
            return;
        }

        public void OnInstantBuild(Bot bot, GroupMessageEvent messageEvent)
        {
            if (PermissionManager.Instance.GetPermission(bot, messageEvent.GroupUin, messageEvent.MemberUin) < Permission.Admin)
            {
                messageEvent.Reply(bot, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text(
                        $"你没有权限使用此命令\n" +
                        $"此命令需要的权限等级为 {Permission.Admin}\n" +
                        $"你的权限等级为 {PermissionManager.Instance.GetPermission(bot, messageEvent.GroupUin, messageEvent.MemberUin)}"));
                return;
            }

            var starbase = starBases.First(x => x.Owner == messageEvent.MemberUin);
            starbase.ShipBuildSequence.ForEach(x => x.BuildTimeMinute = 0);
            starbase.StarBaseBuildSequence.ForEach(x => x.BuildTimeMinute = 0);
            starbase.Flush();
            return;
        }

        public void OnGiveShip(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            if (PermissionManager.Instance.GetPermission(bot, messageEvent.GroupUin, messageEvent.MemberUin) < Permission.Admin)
            {
                messageEvent.Reply(bot, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text(
                        $"你没有权限使用此命令\n" +
                        $"此命令需要的权限等级为 {Permission.Admin}\n" +
                        $"你的权限等级为 {PermissionManager.Instance.GetPermission(bot, messageEvent.GroupUin, messageEvent.MemberUin)}"));
                return;
            }

            var num = 1u;
            var code = "";

            if (args.Count <= 0)
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text("错误: 缺少参数: <shipCode>"));
                return;
            }
            else
            {
                code = args[0];
                if (args.Count > 1)
                {
                    if (!uint.TryParse(args[1], out num) || num < 1)
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 参数错误: {args[0]} => [<num>]."));
                        return;
                    }
                }
            }
            var starbase = starBases.FirstOrDefault(x => x.Owner == messageEvent.MemberUin);
            if (starbase == null)
            {
                starbase = new StarBase(messageEvent.MemberUin, $"{messageEvent.MemberCard} 的基地");
                starBases.Add(starbase);
            }

            var ship = StellaWarDB.Instance.dbConnection.Table<BaseShip>().Where(x => x.Code == code || x.Name == code).ToList().First() ?? null;
            if (ship == null)
                return;
            for (var i = 0; i < num; i++)
            {
                starbase.ShipBuildSequence.Add(ship.Clone());
            }
            starbase.Flush();
            return;
        }
    }
}
