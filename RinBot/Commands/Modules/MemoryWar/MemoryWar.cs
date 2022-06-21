using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using NLog;
using NLog.Targets;
using RinBot.Core.Attributes.Command.Modules;
using RinBot.Core.Attributes.CommandSet;
using RinBot.Core.Components;
using RinBot.Utils;
using RinBot.Utils.Database.Tables;
using System.Text;

namespace RinBot.Commands.Modules.MemoryWar
{
    [CommandSet("内存大战", "com.akulak.memoryWar")]
    internal class MemoryWar : BaseCommand
    {
        MemoryDB db = MemoryDB.Instance;

        List<string> ready_for_attack = new()
        {
            "战斗单元等候命令",
            "下一个目标是什么？",
            "坚守岗位 直至最后一刻",
        };

        int cost_per_unit = 5000;
        int cost_protect = 125;

        int collect_per_unit = 200;
        int maintain_cost_per_attacker = 100;

        int loot_per_attacker = 10000;

        int build_cooldown_minute = 5;

        private static readonly string TAG = "MEMWAR";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        Timer memoryTimer;

        public override void OnInit()
        {
            memoryTimer = new Timer(new TimerCallback(CalcMemoryChange), null, Timeout.Infinite, (int)new TimeSpan(1, 0, 0).TotalMilliseconds);
            memoryTimer.Change((60 - DateTime.Now.Minute) * 60 * 1000, (int)new TimeSpan(1, 0, 0).TotalMilliseconds);
        }

        private void CalcMemoryChange(object value)
        {
            var memoryList = db.dbConnection
                .Table<MemoryInfo>().ToList();

            var userInfo = memoryList.Select(x => x.uin);

            var infoList = UserInfoManager.db
                .Table<UserInfo>()
                .Where(x => userInfo.Contains(x.uin))
                .ToList();

            foreach (var memory in memoryList)
            {
                int total_change = 0;
                total_change += memory.engineer * collect_per_unit;
                total_change -= memory.attacker * maintain_cost_per_attacker;
                total_change -= (memory.attacker + memory.engineer) * cost_protect;
                
                var info = infoList.First(x => x.uin == memory.uin);
                if (info.coin + total_change < 0)
                {
                    if (memory.attacker > 0)
                        memory.attacker--;
                    info.coin = 0;
                }
                else
                    info.coin += total_change;
            }

            UserInfoManager.db.UpdateAll(infoList);
            int count = db.dbConnection.UpdateAll(memoryList);
            Logger.Info($"更新了 {count} 条记录");
        }

#if DEBUG
        [GroupMessageCommand("内存状况更新", new[] { @"^memory-update"})]
        public void OnMemoryUpdate(Bot bot, GroupMessageEvent messageEvent)
        {
            CalcMemoryChange(null);
        }
#endif

        [GroupMessageCommand("内存信息", new[] { @"^memory", @"^内存信息" })]
        public void OnMemoryInfo(Bot bot, GroupMessageEvent messageEvent)
        {
            var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);
            var memory = db.GetUserInfo(messageEvent.MemberUin);

            StringBuilder sb = new();

            sb.AppendLine($"[内存信息]{messageEvent.MemberCard}");
            sb.AppendLine($"拥有内存：{UserInfoManager.CoinToString(info.coin)}\n");

            if (memory.engineer == 0)
            {
                sb.AppendLine($"目前没有任何工程单元正在工作\n");
            }
            else
            {
                sb.AppendLine($"{memory.engineer} 个工程单元正在努力工作");
                sb.AppendLine($"单元平均制造时间：{(int)((float)build_cooldown_minute / (memory.engineer + 1) * 60)} 秒");
                sb.AppendLine($"每小时内存产出：{UserInfoManager.CoinToString(memory.engineer * collect_per_unit)}\n");
            }

            if (memory.attacker == 0)
            {
                sb.AppendLine($"目前没有任何战斗单元正在工作\n");
            }
            else
            {
                sb.AppendLine($"{memory.attacker} 个战斗单元随时准备出发");

                if ((DateTime.Now - memory.lastWar) < new TimeSpan(1, 0, 0))
                {
                    sb.AppendLine($"上一次攻击令我们的战斗单元元气大伤 我们还需要一段时间整备");
                    sb.AppendLine($"预计在 {memory.lastWar.AddHours(1):g} 时整备完毕\n");
                }
                else
                {
                    sb.AppendLine(ready_for_attack[new Random().Next(ready_for_attack.Count)] + "\n");
                }
                sb.AppendLine($"每小时内存消耗：{UserInfoManager.CoinToString(memory.attacker * maintain_cost_per_attacker)}\n");

            }

            if (memory.isProtected)
            {
                sb.AppendLine($"防御系统已开启 正在为 {memory.attacker + memory.engineer} 个单元提供保护");
                sb.AppendLine($"每小时内存消耗：{UserInfoManager.CoinToString((memory.attacker + memory.engineer) * cost_protect)}\n");
            }

            int total_change = 0;
            total_change += memory.engineer * collect_per_unit;
            total_change -= memory.attacker * maintain_cost_per_attacker;
            if (memory.isProtected)
                total_change -= (memory.attacker + memory.engineer) * cost_protect;
            sb.AppendLine($"基地总内存收支：{UserInfoManager.CoinToString(total_change)}");


            if (total_change < 0)
            {
                sb.AppendLine("警告 当前内存收支为赤字");
                sb.AppendLine("如果我们的内存消耗殆尽 每小时都将失去一架战斗单元");
            }

            messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text(sb.ToString()));
        }

        [GroupMessageCommand("防御系统", new[] { @"^defense", @"^防御" })]
        public void OnShield(Bot bot, GroupMessageEvent messageEvent)
        {
            var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);
            var memory = db.GetUserInfo(messageEvent.MemberUin);

            memory.isProtected = !memory.isProtected;
            db.UpdateUserInfo(memory);

            messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"防御系统已{(memory.isProtected ? "开启" : "关闭")}"));
        }

        [GroupMessageCommand("制造战斗单元", new[] { @"^build-attacker\s?([\s\S]+)?", @"^制造战斗单元\s?([\s\S]+)?" })]
        public void OnBuyAttacker(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);
            var memory = db.GetUserInfo(messageEvent.MemberUin);

            var num = 1;

            if (args.Count > 0)
            {
                if (!int.TryParse(args[0], out num) || num <= 0)
                {
                    var reply = $"错误: 参数非法: \"{args[0]}\" => <num>";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
            }


            if (memory.nextBuild > DateTime.Now)
            {
                messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"拒绝 还需等待 {(int)(memory.nextBuild - DateTime.Now).TotalSeconds} 秒才能进行制造"));
                return;
            }
            else
            {
                if (info.coin < cost_per_unit * num)
                {
                    messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"拒绝 制造{num}架战斗单元需要 {UserInfoManager.CoinToString(cost_per_unit)}\n而你只有 {UserInfoManager.CoinToString(info.coin)}"));
                    return;
                }
                else if (num > memory.engineer + 1)
                {
                    messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"拒绝 制造{num}架战斗单元超过了最大产能上限: {memory.engineer + 1}"));
                    return;
                }
                else
                {
                    info.coin -= cost_per_unit * num;
                    memory.nextBuild = DateTime.Now.AddMinutes((float)build_cooldown_minute * num / (memory.engineer + 1));
                    memory.attacker += num;

                    messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"最后的组装工作已经完成 {num}架全新的战斗单元离开了船坞\n" +
                            $"它已经准备好保卫一切资产 或是侵略...\n" +
                            $"下一次制造将在 {(int)(memory.nextBuild - DateTime.Now).TotalSeconds} 秒后可用"));
                    

                    UserInfoManager.UpdateUserInfo(info);
                    db.UpdateUserInfo(memory);
                    return;
                }
            }
        }

        [GroupMessageCommand("制造工程单元", new[] { @"^build-engineer\s?([\s\S]+)?", @"^制造工程单元\s?([\s\S]+)?" })]
        public void OnBuyEngineer(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);
            var memory = db.GetUserInfo(messageEvent.MemberUin);

            var num = 1;

            if (args.Count > 0)
            {
                if (!int.TryParse(args[0], out num) || num <= 0)
                {
                    var reply = $"错误: 参数非法: \"{args[0]}\" => <num>";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
            }


            if (memory.nextBuild > DateTime.Now)
            {
                messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"拒绝 还需等待 {(int)(memory.nextBuild - DateTime.Now).TotalSeconds} 秒才能进行制造"));
                return;
            }
            else
            {
                if (info.coin < cost_per_unit * num)
                {
                    messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"拒绝 制造{num}架工程单元需要 {UserInfoManager.CoinToString(cost_per_unit)}\n而你只有 {UserInfoManager.CoinToString(info.coin)}"));
                    return;
                }
                else if (num > memory.engineer + 1)
                {
                    messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"拒绝 制造{num}架工程单元超过了最大产能上限: {memory.engineer + 1}"));
                    return;
                }
                else
                {
                    info.coin -= cost_per_unit * num;
                    memory.nextBuild = DateTime.Now.AddMinutes((float)build_cooldown_minute * num / (memory.engineer + 1));
                    memory.engineer += num;

                    messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"引擎轰鸣 {num}架全新的工程单元从工厂中驶出\n" +
                            $"很快它就会带来源源不断的内存收益 并且辅助其余单位的建设\n" +
                            $"下一次制造将在 {(int)(memory.nextBuild - DateTime.Now).TotalSeconds} 秒后可用"));
                    

                    UserInfoManager.UpdateUserInfo(info);
                    db.UpdateUserInfo(memory);
                    return;
                }
            }
        }
        
        //To war!
        [GroupMessageCommand("进攻", new[] { @"^attack\s?([\s\S]+)?", @"^攻击\s?([\s\S]+)?" })]
        public void OnAttack(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var uin = messageEvent.MemberUin;
            uint target = 0u;
            var targetName = "";
            UserInfo? targetInfo;

            var info = UserInfoManager.GetUserInfo(uin);
            var memory = db.GetUserInfo(uin);

            if ((DateTime.Now - memory.lastWar) < new TimeSpan(1, 0, 0))
            {
                messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"上一次攻击令我们的战斗单元元气大伤 我们还需要一段时间整备\n预计在 {memory.lastWar.AddHours(1):g} 时可以准备完毕"));
                return;
            }

            if (memory.attacker == 0)
            {
                messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"没有可用的战斗单元用于进攻"));
                return;
            }

            if (args.Count > 0)
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

                targetInfo = UserInfoManager.GetUserInfo(target, false);
                if (targetInfo == null)
                {
                    messageEvent.Reply(bot, new MessageBuilder()
                                .Add(ReplyChain.Create(messageEvent.Message))
                                .Text($"错误：攻击目标不存在"));
                    return;
                }
            }
            else
            {
                var list = bot.GetGroupMemberList(messageEvent.GroupUin).Result;
                var targetI = list.ElementAt(new Random().Next(list.Count));
                target = targetI.Uin;
                targetName = targetI.NickName;
                targetInfo = UserInfoManager.GetUserInfo(target);
            }

            var targetMemory = db.GetUserInfo(target);
            


            StringBuilder sb = new();

            sb.AppendLine($"进攻部队准备攻击 {targetName}");

            if (targetMemory.isProtected)
            {
                sb.AppendLine($"对方的防御系统正在运行");
                if (memory.attacker > 10)
                {
                    int loss = new Random().Next(0, (int)(memory.attacker * 0.8f));
                    memory.attacker -= loss;
                    sb.AppendLine($"我们的战斗单元强行突破了防御系统 但是有 {loss} 架战斗单元被击落");

                }
                else
                {
                    sb.AppendLine($"我们的战斗单元无法突破 只能撤退");
                    messageEvent.Reply(bot, new MessageBuilder()
                                    .Add(ReplyChain.Create(messageEvent.Message))
                                    .Text(sb.ToString()));
                    return;
                }
            }

            sb.AppendLine($"对方拥有 {targetMemory.attacker} 个战斗单元");
            sb.AppendLine($"我方拥有 {memory.attacker} 个战斗单元\n");

            float accuracy = 0.2f;
            int counter = 1;

            int total_attack = memory.attacker;
            int total_defense = targetMemory.attacker;

            while (targetMemory.attacker > 0 && memory.attacker > 0)
            {
                counter++;

                int attack_loss = targetMemory.attacker * new Random().Next((int)Math.Round(targetMemory.attacker * accuracy), targetMemory.attacker);
                int defense_loss = memory.attacker * new Random().Next((int)Math.Round(memory.attacker * accuracy), memory.attacker);

                if (attack_loss > memory.attacker)
                    attack_loss = memory.attacker;

                if (defense_loss > memory.attacker)
                    defense_loss = memory.attacker;

                memory.attacker -= attack_loss;
                if (memory.attacker < 0)
                    memory.attacker = 0;

                targetMemory.attacker -= defense_loss;
                if (targetMemory.attacker < 0)
                    targetMemory.attacker = 0;

                if (accuracy < 1f)
                    accuracy += 0.1f;
            }

            sb.AppendLine($"战斗持续了 {counter} 天");

            if (memory.attacker > 0)
            {
                sb.AppendLine($"对方的战斗单元消耗殆尽 我们取得了胜利");
            }
            else if (targetMemory.attacker > 0)
            {
                sb.AppendLine($"我们的战斗单元消耗殆尽 进攻失败了");
            }
            else
            {
                sb.AppendLine("交战双方的战斗单元都消耗殆尽 这场战争没有赢家");
            }

            sb.AppendLine("");

            sb.AppendLine($"对方损失 {total_defense - targetMemory.attacker} 个战斗单元");
            sb.AppendLine($"我方损失 {total_attack - memory.attacker} 个战斗单元\n");


            if (memory.attacker > 0)
            {
                int collector_loss = new Random().Next(targetMemory.engineer);
                if (collector_loss > 0)
                {
                    targetMemory.engineer -= collector_loss;
                    sb.AppendLine($"有 {collector_loss} 个工程单元尝试抵抗 被无情的摧毁了");
                    var collector_revolt = new Random().Next(collector_loss);
                    if (collector_revolt > 0)
                    {
                        if (memory.attacker - collector_revolt < 1)
                            collector_revolt = memory.attacker - 1;
                        memory.attacker -= collector_revolt;
                        sb.AppendLine($"但这次抵抗也摧毁了我们 {collector_revolt} 个战斗单元");
                    }
                }
                int loot = memory.attacker * loot_per_attacker;
                if (targetInfo.coin < loot)
                    loot = targetInfo.coin;
                targetMemory.isProtected = true;

                sb.AppendLine($"我们的 {memory.attacker} 个战斗单元成功洗劫了对方 并且带走了 {UserInfoManager.CoinToString(loot)}");
                
                sb.AppendLine($"在战斗单元驶离后 {targetName} 启动了紧急防御系统");
            }
            else
            {
                memory.isProtected = true;
                sb.AppendLine($"最后一架战斗单元在被摧毁前传来战败的信息 我们启动了紧急防御系统");
            }

            memory.lastWar = DateTime.Now;

            UserInfoManager.UpdateUserInfo(info);
            UserInfoManager.UpdateUserInfo(targetInfo);
            db.UpdateUserInfo(memory);
            db.UpdateUserInfo(targetMemory);

            messageEvent.Reply(bot, new MessageBuilder()
                                    .Add(ReplyChain.Create(messageEvent.Message))
                                    .Text(sb.ToString()));
            return;
        }
    }
}
