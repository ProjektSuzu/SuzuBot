using RinBot.Command.Arcaea;
using RinBot.Core.Component.Command.CustomAttribute;
using RinBot.Core.Component.Event;
using RinBot.Core.Component.Message;
using RinBot.Core.Component.Message.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Command.Apex
{
    [Module("Apex", "org.akulak.apex")]
    internal class Apex
    {
        [Command("Apex", new[] { @"^apex\s?(.+)?" }, (int)MatchingType.Regex, ReplyType.Reply)]
        public RinMessageChain OnApex(RinEvent e, List<string> args)
        {
            if (args.Count == 0)
            {
                return OnUserStatus(e, new());
            }

            string funcName = args[0];
            args = args.Skip(1).ToList();

            switch (funcName)
            {
                case "stat":
                case "status":
                    return OnUserStatus(e, args);

                case "predator":
                case "猎杀":
                case "冲猎":
                    return OnPredator(e);

                case "bind":
                    return OnBind(e, args);

                case "unbind":
                    return OnUnBind(e);

                default:
                    var chain = new RinMessageChain();
                    chain.Add(TextChain.Create($"[Apex]\n找不到功能: {funcName}"));
                    return chain;
            }
        }

        public RinMessageChain OnUserStatus(RinEvent e, List<string> args)
        {
            var chain = new RinMessageChain();
            chain.Add(TextChain.Create("[Apex]UserStatus\n"));
            StringBuilder stringBuilder = new();

            PlayerStats playerQueryResult = null;
            ApexBindInfo info = null;
            if (args.Count <= 0)
            {
                info = ApexUserDB.Instance.GetBindInfo(e.SenderId, e.EventSourceType);
                if (info == null)
                {
                    stringBuilder.AppendLine("未查询到用户的绑定信息");
                    stringBuilder.AppendLine("请先使用");
                    stringBuilder.AppendLine("/apex bind <userName>");
                    stringBuilder.AppendLine("进行绑定 或使用");
                    stringBuilder.AppendLine("/apex status <userName>");
                    stringBuilder.AppendLine("直接查询");
                    chain.Add(TextChain.Create(stringBuilder.ToString()));
                    return chain;
                }
                else
                {
                    playerQueryResult = ApexAPI.Instance.GetPlayerStatsByUID(info.PlayerId).Result;
                }
            }
            else
            {
                var userName = String.Join(' ', args);
                playerQueryResult = ApexAPI.Instance.GetPlayerStatsByName(userName).Result;
            }

            if (playerQueryResult == null)
            {
                stringBuilder.AppendLine("查询时发生了错误\n服务器连接错误");
                chain.Add(TextChain.Create(stringBuilder.ToString()));
                return chain;
            }
            else if (playerQueryResult.Error != null)
            {
                stringBuilder.AppendLine("查询时发生了错误");
                stringBuilder.AppendLine(playerQueryResult.Error);
                chain.Add(TextChain.Create(stringBuilder.ToString()));
                return chain;
            }

            var legendName = playerQueryResult.legends.selected.LegendName;
            var selectedLegend = playerQueryResult.legends.all.FirstOrDefault(x => x.Key == legendName).Value;
            var reply =
        $"用户名: {playerQueryResult.global.name}\n" +
        $"等级: {playerQueryResult.global.level}\n" +
        $"当前在线状态: {(playerQueryResult.realtime.isInGame == 1 ? "游戏中" : playerQueryResult.realtime.isOnline == 1 ? "在线" : "离线")}\n" +
        $"当前排位段位: {GetRankName(playerQueryResult.global.rank.rankName)} {playerQueryResult.global.rank.rankDiv}\n" +
        $"当前排位积分: {playerQueryResult.global.rank.rankScore} RP\n" +
        $"当前竞技场段位: {GetRankName(playerQueryResult.global.arena.rankName)} {playerQueryResult.global.arena.rankDiv}\n" +
        $"当前竞技场积分: {playerQueryResult.global.arena.rankScore} AP\n" +
        $"================\n" +
        $"当前选择传奇: {legendName}";

            if (selectedLegend.data != null)
            {
                reply += "\n追踪器:\n";
                foreach (var data in selectedLegend.data)
                {
                    reply += $"{data.name}: {data.value}\n";
                }
            }
            chain.Add(TextChain.Create(reply));
            return chain;
        }

        public RinMessageChain OnBind(RinEvent e, List<string> args)
        {
            RinMessageChain chain = new RinMessageChain();
            ApexBindInfo bindInfo = ApexUserDB.Instance.GetBindInfo(e.SenderId, e.EventSourceType);
            if (bindInfo != null)
            {
                chain.Add(TextChain.Create($"[Apex]\n已存在绑定信息 如需更换绑定 请先使用\n/apex unbind\n进行解绑\n{bindInfo.PlayerName}({bindInfo.PlayerId})"));
                return chain;
            }

            string userName;
            if (args.Count < 1)
            {
                chain.Add(TextChain.Create($"[Arcaea]\n缺少参数 <userName>"));
                return chain;
            }
            else
            {
                userName = String.Join(' ', args);
            }

            var playerQueryResult = ApexAPI.Instance.GetPlayerStatsByName(userName).Result;
            if (playerQueryResult == null)
            {
                chain.Add(TextChain.Create("查询时发生了错误\n服务器连接错误"));
                return chain;
            }
            else if (playerQueryResult.Error != null)
            {
                chain.Add(TextChain.Create($"查询时发生了错误\n{playerQueryResult.Error}"));
                return chain;
            }

            bindInfo = new()
            {
                UserId = e.SenderId,
                UserType = e.EventSourceType,
                PlayerId = playerQueryResult.global.uid,
                PlayerName = playerQueryResult.global.name,
            };
            ApexUserDB.Instance.UpdateBindInfo(bindInfo);

            chain.Add(TextChain.Create($"[Apex]\n已成功绑定到用户\n{bindInfo.PlayerName}({bindInfo.PlayerId})"));
            return chain;
        }

        public RinMessageChain OnUnBind(RinEvent e)
        {
            RinMessageChain chain = new RinMessageChain();
            ApexBindInfo bindInfo = ApexUserDB.Instance.GetBindInfo(e.SenderId, e.EventSourceType);
            if (bindInfo == null)
            {
                chain.Add(TextChain.Create($"[Apex]\n未存在绑定信息"));
                return chain;
            }
            ApexUserDB.Instance.DeleteBindInfo(bindInfo);
            chain.Add(TextChain.Create($"[Apex]\n已解除绑定"));
            return chain;
        }

        public RinMessageChain OnPredator(RinEvent e)
        {
            RinMessageChain chain = new RinMessageChain();
            ApexBindInfo bindInfo = ApexUserDB.Instance.GetBindInfo(e.SenderId, e.EventSourceType);

            PredatorInfo result = ApexAPI.Instance.GetPredatorInfo().Result;

            if (result == null)
            {
                chain.Add(TextChain.Create("查询时发生了错误\n服务器连接错误"));
                return chain;
            }

            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("[Apex]");
            stringBuilder.AppendLine($"排位最低冲猎分数: {result.RankPoint} RP");
            stringBuilder.AppendLine($"竞技场最低冲猎分数: {result.RankPoint} AP");
            if (bindInfo != null)
            {
                var playerQueryResult = ApexAPI.Instance.GetPlayerStatsByUID(bindInfo.PlayerId).Result;
                if (playerQueryResult != null && playerQueryResult.Error == null)
                {
                    stringBuilder.AppendLine($"============");
                    stringBuilder.AppendLine($"{playerQueryResult.global.name}");
                    stringBuilder.AppendLine($"排位分数: {playerQueryResult.global.rank.rankScore} RP");
                    stringBuilder.AppendLine($"竞技场分数: {playerQueryResult.global.arena.rankName} AP");
                }
            }

            chain.Add(TextChain.Create(stringBuilder.ToString()));
            return chain;
        }

        string GetRankName(string rank)
        {
            switch (rank)
            {
                case "Bronze":
                    return "青铜";
                case "Silver":
                    return "白银";
                case "Gold":
                    return "黄金";
                case "Platinum":
                    return "铂金";
                case "Diamond":
                    return "钻石";
                case "Master":
                    return "大师";
                case "Apex Predator":
                    return "APEX 猎杀者";
                default:
                    return "未定级";
            }
        }
    }
}
