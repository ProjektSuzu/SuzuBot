using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.Components;
using System.Diagnostics;

namespace ProjektRin.Commands.Modules.Apex
{
    [CommandSet("Apex查分", "com.akulak.apexProbe")]
    internal class ApexCommand : BaseCommand
    {
        public override string Help =>
            $"[Apex]\n" +
            $"/apex help\n" +
            $"  打印帮助信息\n" +
            $"/apex [<userId>]\n" +
            $"  查询某个用户的 Apex Legend 账户信息\n" +
            $"  目前只能查询 PC 平台\n" +
            $"/apex predator\n" +
            $"  查询最低冲猎段位分数要求\n" +
            $" 快捷名: \n" +
            $"/apex 猎杀\n" +
            $"/apex 冲猎\n" +
            $"\n" +
            $"/apex bind <userId>\n" +
            $"  将当前QQ号与一个用户名绑定\n" +
            $"  将来查询时将不需要手动输入用户名\n" +
            $"/apex unbind\n" +
            $"  将当前QQ号与一个用户名解绑\n" +
            $"\n" +
            $"  userId  Origin 用户名  注意不是 Steam 用户名\n";

        private ApexAPI api = ApexAPI.Instance;
        private ApexUserDB db = ApexUserDB.Instance;

        public override void OnInit()
        {
        }

        [GroupMessageCommand("Apex", @"^apex\s?([\s\S]+)?")]
        public void OnApex(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            if (args.Count > 0)
            {
                var funcName = args[0];
                var newArgs = args.Skip(1).ToList();
                switch (funcName)
                {
                    case "help":
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text(Help)
                        );
                        return;

                    case "bind":
                        OnBind(bot, messageEvent, newArgs);
                        return;

                    case "unbind":
                        OnUnBind(bot, messageEvent, newArgs);
                        return;

                    case "predator":
                    case "冲猎":
                    case "猎杀":
                        OnApexPredatorRequirement(bot, messageEvent, newArgs);
                        return;


                    default:
                        OnInfo(bot, messageEvent, args);
                        return;
                }
            }
            else
            {
                OnInfo(bot, messageEvent, args);
                return;
            }
        }

        public void OnApexPredatorRequirement(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            Process p = new Process();
            p.StartInfo.FileName = "python3";
            p.StartInfo.Arguments = Path.Combine(BotManager.resourcePath, "ApexProbe/pfp.py");
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            p.WaitForExit();
            var result = p.StandardOutput.ReadToEnd();
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);

            var reply =
                $"[APEX查分器]\n" +
                $"当前排位最低冲猎分数: {json["BR"]} RP\n" +
                $"当前竞技场最低冲猎分数: {json["Arena"]} AP";

            var info = db.GetByUin(messageEvent.MemberUin);

            if (info != null)
            {
                var stats = api.GetPlayerStats(info.UserId).Result;
                if (stats != null && stats.Error == null)
                {
                    reply += $"\n当前排位分数: {stats.global.rank.rankScore} RP\n" +
                             $"当前竞技场分数: {stats.global.arena.rankScore} AP\n";
                }
            }

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text(reply)
            );
            return;
        }

        public void OnUnBind(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            var info = db.GetByUin(messageEvent.MemberUin);
            if (info == null)
            {
                reply = "错误: 当前QQ号不存在绑定的记录.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            else
            {
                db.Remove(messageEvent.MemberUin);
                reply = $"U{messageEvent.MemberUin} => ∅    解绑成功.\n";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply).Add(ReplyChain.Create(messageEvent.Message)));
                return;
            }
        }

        public void OnBind(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string userId;
            string reply;
            if (args.Count > 0)
            {
                userId = args[0];
            }
            else
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text($"错误: 缺少参数: <userId>\n" +
                          $"如果你是第一次使用, 请使用/apex help 查看帮助信息")
                );
                return;
            }

            var info = db.GetByUin(messageEvent.MemberUin);
            if (info != null)
            {
                reply = "错误: 当前QQ号已存在一个绑定的记录.\n" +
                        "如需更换绑定, 请先使用 /apex unbind 解绑.\n" +
                        $"U{info.Uin} => {info.UserId}.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply).Add(ReplyChain.Create(messageEvent.Message)));
                return;
            }

            var newInfo = new ApexUserDB.ApexUserInfo()
            {
                UserId = userId,
                Uin = messageEvent.MemberUin
            };
            db.Insert(newInfo);
            reply = $"绑定成功\n" +
                    $"U{messageEvent.MemberUin} => {newInfo.UserId}.";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply).Add(ReplyChain.Create(messageEvent.Message)));
            return;
        }

        public void OnInfo(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string userId;
            if (args.Count > 0)
            {
                userId = args[0];
            }
            else
            {
                var info = db.GetByUin(messageEvent.MemberUin);
                if (info != null)
                {
                    userId = info.UserId;
                }
                else
                {
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"错误: 缺少参数: <userId>\n" +
                              $"如果你是第一次使用, 请使用/apex help 查看帮助信息")
                    );
                    return;
                }
            }

            var result = api.GetPlayerStats(userId).Result;

            if (result == null)
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text("获取时发生错误: 获取失败")
                );
                return;
            }

            if (result.Error != null)
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text($"获取时发生错误: {result.Error}")
                );
                return;
            }

            var legendName = result.legends.selected.LegendName;
            var selectedLegend = result.legends.all.FirstOrDefault(x => x.Key == legendName).Value;

            var reply = "[APEX查分器]\n";
            reply +=
                $"用户名: {result.global.name}\n" +
                $"等级: {result.global.level}\n" +
                $"当前在线状态: {(result.realtime.isInGame == 1 ? "正在游戏" : (result.realtime.isOnline == 1) ? "在线" : "离线")}\n" +
                $"当前排位段位: {GetRankName(result.global.rank.rankName)} {result.global.rank.rankDiv}\n" +
                $"当前排位积分: {result.global.rank.rankScore} RP\n" +
                $"当前竞技场段位: {GetRankName(result.global.arena.rankName)} {result.global.arena.rankDiv}\n" +
                $"当前竞技场积分: {result.global.arena.rankScore} AP\n" +
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



            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text(reply)
            );
            return;
        }

        String GetRankName(string rank)
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
