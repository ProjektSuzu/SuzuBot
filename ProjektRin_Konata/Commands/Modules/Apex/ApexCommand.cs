using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;

namespace ProjektRin.Commands.Modules.Apex
{
    [CommandSet("Apex查分", "com.akulak.apexProbe")]
    internal class ApexCommand : BaseCommand
    {
        public override string Help =>
            $"[Apex]测试版\n" +
            $"/apex <userId>" +
            $"  查询某个用户的 Apex Legend 账户信息\n" +
            $"  目前只能查询 Origin 平台 未来将支持 Xbox 和 PSN 平台账户的查询\n" +
            $"\n" +
            $"  userId  Origin 用户名  注意不是 Steam 用户名\n";

        private ApexAPI api = ApexAPI.Instance;
        
        public override void OnInit()
        {
        }

        [GroupMessageCommand("Apex", @"^apex\s?([\s\S]+)?")]
        public void OnApex(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string userId;
            if (args.Count > 0)
            {
                userId = args[0];
            }
            else
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text($"错误: 缺少参数: <userId>")
                );
                return;
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

            if (result.errors != null && result.errors.Count > 0)
            {
                var code = result.errors.First().code;
                switch (code)
                {
                    case "CollectorResultStatus::NotFound":
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"获取时发生错误: 找不到用户 {userId}\n" +
                                  $"目前只支持查询Origin平台的用户名 请确认输入的是Origin平台的用户名")
                        );
                        return;
                    }

                    default:
                    {
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"获取时发生错误: 未知错误")
                        );
                        return;
                    }
                }
            }

            var reply =
                $"[APEX]试验性查分器\n" +
                $"用户名: {result.data.platformInfo.platformUserIdentifier}\n";

            foreach (var segment in result.data.segments)
            {
                switch (segment.type)
                {
                    case "overview":
                    {
                        var level = segment.stats["level"];
                        var rankScore = segment.stats["rankScore"];
                        var arenaRankScore = segment.stats["arenaRankScore"];

                            reply +=
                                $"等级: {level.displayValue}\n" +
                                $"排名积分: {rankScore.displayValue}\n" +
                                $"竞技场积分: {arenaRankScore.displayValue}\n" +
                                $"================\n";
                            break;
                    }

                    case "legend":
                    {
                        var metadata = segment.metadata;
                        if (!metadata.isActive) break;

                        var legendName = metadata.name;

                        reply +=
                            $"当前传奇: {legendName}\n" +
                            $"追踪器:\n";

                        foreach (var pairStat in segment.stats)
                        {
                            reply +=
                                $"{pairStat.Value.displayName}: {pairStat.Value.displayValue}\n";
                        }

                        reply +=
                            $"================\n";
                        break;
                    }

                    default: break;
                }
            }

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text(reply)
            );
            return;

        }
    }
}
