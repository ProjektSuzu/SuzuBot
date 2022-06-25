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

                case "cancel-build-ship":
                    OnCancelShipBuild(bot, messageEvent, args);
                    return;

                case "base-rename":
                    OnBaseRename(bot, messageEvent, args);
                    return;

                case "simulate":
                    OnSimulate(bot, messageEvent);
                    return;

                case "instant-build":
                    OnInstantBuild(bot, messageEvent);
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
            sb.AppendLine("[群星争霸]基地信息");
            sb.AppendLine($"{starbase.Name}");
            sb.AppendLine($"基地等级: {starbase.Level}");
            sb.AppendLine($"内存储量: {UserInfoManager.CoinToString(info.coin)}");
            sb.AppendLine($"内存收支: {UserInfoManager.CoinToString(starbase.CalcMemoryBalance())}");

            sb.AppendLine($"模块容量: {starbase.Modules.Count()}/{starbase.MaxModuleCapacity}");

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

                    sb.Append($"--{ship.Name}: {count}");
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

                    sb.Append($"--{ship.Name}: {count}");
                }
                sb.AppendLine();
                sb.AppendLine($"预计在 {starbase.CalcRepairCompleteDate():g} 全部维修完成");
                sb.AppendLine();
            }

            sb.AppendLine($"舰船制造队列: {starbase.ShipBuildSequence.Count}");
            if (starbase.ShipBuildSequence.Count > 0)
            {
                int engineerCount = starbase.AllShip.Where(x => x.Code == "engineer").Count();
                int dockModuleCount = starbase.Modules.Where(x => x.ID == "dock").Count();
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
            sb.AppendLine($"{messageEvent.MemberCard} 对 {targetName}");
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

        public void OnCancelShipBuild(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var num = 1;
            var starbase = starBases.First(x => x.Owner == messageEvent.MemberUin);
            var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);

            if (args.Count > 0)
            {
                if (!int.TryParse(args[1], out num) || num < 1)
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
            starbase.Flush();
            return;
        }
    }
}
