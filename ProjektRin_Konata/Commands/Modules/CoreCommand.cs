using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using NLog;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.Components;
using ProjektRin.Utils.BuildStamp;
using ProjektRin.Utils.Database.Tables;
using System.Diagnostics;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("核心功能", "com.akulak.core")]
    internal class CoreCommand : BaseCommand
    {
        private GroupManager groupManager;
        private CommandManager commandManager;

        private static readonly string TAG = "CORECMD";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public string Introduction =>
            $"[自我介绍]\n" +
            $"这里是铃, 是基于 .NET 下的 Konata 框架\n" +
            $"由 AkulaKirov 开发和维护的机器人\n" +
            $"如果想要使用铃, 请使用 \"/\" \"铃酱\" 或者 At 的方式, 并加上想要调用的命令即可\n" +
            $"铃目前处在开发活跃期, 可能会有很多功能上的变动\n" +
            $"所有的命令帮助可以在 /help 里查看, 虽然可能会有点复杂\n" +
            $"但是这也是为了更全面的解释功能用法, 还请各位见谅\n" +
            $"目前铃还有很多需要完善的地方, 如果有好的建议可以和开发者说\n" +
            $"期待今后和你们共度的时光 ☆ ～('▽^人)";

        public string Announcement =
            $"[开发者公告]\n" +
            $"我也不知道我写了个啥玩意\n" +
            $"目前没有写私聊功能, 因为容易被tx封\n" +
            $"自动入群也没有, 如果, 啊, 想拉到别的群的, 吱我一声就行了(1785416538)\n" +
            $"欢迎大家帮我测bug, 滥用的就算了\n" +
            $"Take care of yourself, and be well.\n" +
            $"DE BG5UZP 73 TU\n" +
            $"20220318\n";

        public override string Help =>
            "[ProjektRin]核心命令\n" +
            "/help [<commandSet>]      查看帮助文本\n" +
            "   commandSet             指定的命令集名字\n" +
            "\n" +
            "/cmdctl                   管理命令集启用/禁用等功能\n" +
            "                          具体使用方法请使用 /cmdctl -h 查阅\n" +
            "\n" +
            "/pay <at / uin> <amount>  向指定账户打款\n" +
            "   at                     目标用户的At\n" +
            "   uin                    目标用户的QQ号\n" +
            "   amount                 打款金额\n" +
            "\n" +
            "/ban <uin>                封禁目标账户, 使Bot不再响应对方的任何指令\n" +
            "   uin                    目标的QQ号\n" +
            "\n" +
            "/info [<uin>]             查询用户的信息\n" +
            "   uin                    目标的QQ号\n" +
            "\n" +
            "/passive [<groupUin>]     启动被动模式\n" +
            "                          使Bot只能以At或称呼的形式调用命令\n" +
            "                          并且不再响应以 \'/\' 开头的命令\n" +
            "   groupUin               目标群的群号\n" +
            "\n" +
            "/reload                   重新载入所有命令\n" +
            "\n" +
            "/status                   打印运行状态信息\n" +
            "";

        public override void OnInit()
        {
            groupManager = GroupManager.Instance;
            commandManager = CommandManager.Instance;
        }
        public override void OnDisable() { }

        [GroupMessageCommand("帮助", new[] { @"^help\s?([\s\S]+)?", @"^帮助\s?([\s\S]+)?" })]
        public void OnHelp(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            string? setName = "";
            MultiMsgChain? multiReply = MultiMsgChain.Create();
            if (args.Count > 0)
            {
                setName = args[0];
            }

            if (setName == "")
            {
                multiReply.AddMessage(new MessageStruct(bot.Uin, bot.Name, new MessageBuilder(Introduction).Build()));
                multiReply.AddMessage(new MessageStruct(bot.Uin, bot.Name, new MessageBuilder(Announcement).Build()));
                multiReply.AddMessage(new MessageStruct(bot.Uin, bot.Name, new MessageBuilder(Help).Build()));

                foreach (KeyValuePair<(CommandSet, BaseCommand), List<(Command handler, System.Reflection.MethodInfo method)>> set in commandManager.CmdSets)
                {
                    if (set.Key.Item1.Name == "核心功能")
                    {
                        continue;
                    }

                    string? help = set.Key.Item2.Help;
                    MessageBuilder? message = new MessageBuilder(help);
                    multiReply.AddMessage(new MessageStruct(bot.Uin, bot.Name, message.Build()));
                }
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(multiReply));
                return;
            }
            else
            {
                if (!commandManager.CmdSets.Any(x => x.Key.Item1.Name.Equals(setName)))
                {
                    reply = $"错误: 找不到命令集: \"{setName}\".";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
                KeyValuePair<(CommandSet, BaseCommand), List<(Command handler, System.Reflection.MethodInfo method)>> set = commandManager.CmdSets.Where(x => x.Key.Item1.Name == setName).First();
                string? help = set.Key.Item2.Help;
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(help));
                return;
            }
        }

        [GroupMessageCommand("公告", new[] { @"^announce\s?([\s\S]+)?", @"^公告\s?([\s\S]+)?" }, PermissionManager.Permission.Root)]
        public void OnAnnouncement(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var content = 
                $"  [开发者公告]\n" +
                $"{String.Join(" ", args)}\n\n" +
                $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";

            Announcement = content;

            foreach (var group in bot.GetGroupList().Result)
            {
                if (GroupManager.Instance.IsPassiveMode(group.Uin))
                    continue;
                bot.SendGroupMessage(group.Uin, new MessageBuilder(content));
            }

            return;
        }

        [GroupMessageCommand("反馈", new[] { @"^feedback\s?([\s\S]+)?", @"^反馈\s?([\s\S]+)?" })]
        public void OnFeedback(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var content =
                $"[反馈]\n" +
                $"来自 G{messageEvent.GroupUin}:U{messageEvent.MemberUin}\n\n" +
                $"{String.Join(" ", args)}\n\n" +
                $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";

            
            bot.SendGroupMessage(644504300, new MessageBuilder(content));
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("已收到你的反馈"));

            return;
        }

        [GroupMessageCommand("支付", new[] { @"^pay\s?([\s\S]+)?", @"^转账\s?([\s\S]+)?" })]
        public void OnPay(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            uint uin = messageEvent.MemberUin;
            UserInfo? info = UserInfoManager.GetUserInfo(uin);


            if (args.Count < 2)
            {
                reply = $"错误: 参数不足: <targetUin> <amount>.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            if (!uint.TryParse(args[0], out uint target))
            {
                AtChain? atChain = (AtChain?)messageEvent.Message.Chain.FirstOrDefault(x => x is AtChain);
                if (atChain == null)
                {
                    reply = $"错误: 参数错误: {args[0]} => <targetUin>.";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
                target = atChain.AtUin;
            }

            if (target == uin)
            {
                reply = $"错误: 支付目标不能是自己.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            UserInfo? targetInfo = UserInfoManager.GetUserInfo(target, false);
            if (targetInfo == null)
            {
                reply = $"错误: 目标用户不存在: U{target}.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            if (!uint.TryParse(args[1], out uint amount))
            {
                reply = $"错误: 参数错误: {args[1]} => <amount>.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            if (info.coin < amount)
            {
                reply =
                    $"你的内存不足\n" +
                    $"尝试支付 {UserInfoManager.CoinToString(amount)}, 而你只有 {UserInfoManager.CoinToString(info.coin)}.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            info.coin -= amount;
            targetInfo.coin += amount;

            UserInfoManager.UpdateUserInfo(info);
            UserInfoManager.UpdateUserInfo(targetInfo);

            reply =
                    $"转账成功: {UserInfoManager.CoinToString(amount)} => U{target}\n" +
                    $"当前余额: {UserInfoManager.CoinToString(info.coin)}.";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
            return;
        }

        [GroupMessageCommand("打钱", new[] { @"^give\s?([0-9]+)?", @"^给我\s?([0-9]+)?", @"^v我\s?([0-9]+)?" }, PermissionManager.Permission.Root)]
        public void OnGive(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            uint uin = messageEvent.MemberUin;
            UserInfo? info = UserInfoManager.GetUserInfo(uin);

            uint coin = 0u;
            if (args.Count > 0)
            {
                if (!uint.TryParse(args[0], out coin))
                {
                    reply = $"错误: 参数非法: \"{args[0]}\" => <amount>.";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
            }

            info.coin += coin;
            UserInfoManager.UpdateUserInfo(info);
            reply = $"已经向 U{uin} 的账户添加 {UserInfoManager.CoinToString(coin)}.";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
            return;
        }

        [GroupMessageCommand("封禁", new[] { @"^ban\s?([\s\S]+)?" })]
        public void OnBan(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            uint uin = 0u;
            if (args.Count > 0)
            {
                if (!uint.TryParse(args[0], out uin))
                {
                    reply = $"错误: 参数非法: \"{args[0]}\" => <uin>.";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
            }

            if (uin == 0u)
            {
                reply = $"错误: 缺少参数: <uin>.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            if (PermissionManager.Instance.IsAdmin(uin))
            {
                reply = $"U{uin} 无法被封禁.";
                Logger.Info($"U{messageEvent.MemberUin} try to ban an admin.");
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            if (uin == messageEvent.MemberUin)
            {
                reply = $"不能封禁自己.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            UserInfo? info = UserInfoManager.GetUserInfo(uin);
            info.isBanned = !info.isBanned;
            UserInfoManager.UpdateUserInfo(info);

            reply = $"U{uin} 已被 {(info.isBanned ? "封禁" : "解封")}.";
            Logger.Info($"U{uin} => {(info.isBanned ? "Ban" : "Unban")}.");
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
            return;
        }

        [GroupMessageCommand("用户信息", new[] { @"^info\s?([\s\S]+)?", @"^信息\s?([\s\S]+)?" })]
        public void OnUserInfo(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            uint uin = messageEvent.MemberUin;
            bool create = true;
            if (args.Count > 0)
            {
                if (!uint.TryParse(args[0], out uin))
                {
                    reply = $"错误: 参数非法: \"{args[0]}\" => <uin>.";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
                create = false;
            }
            UserInfo? info = UserInfoManager.GetUserInfo(uin, create);
            if (info == null)
            {
                reply = $"错误: 找不到用户: \"U{uin}\".";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            reply =
                $"[UserInfo]\n" +
                $"用户: {info.uin}\n" +
                $"内存: {UserInfoManager.CoinToString(info.coin)}\n" +
                $"等级: {info.level}\n" +
                $"经验: {info.exp} exp\n" +
                $"距离下一等级: {UserInfoManager.LevelToExp(info.level) - info.exp} exp";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
            return;
        }

        [GroupMessageCommand("命令集控制", @"^cmdctl\s?([\s\S]+)?")]
        public void OnCommandControl(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            uint groupUin = messageEvent.GroupUin;
            bool? action = null;
            bool global = false;
            string help =
                $"[CommandControl]\n" +
                $"用法: /cmdctl <enable/disable> [-opts] [<args>]\n\n" +
                $"启用或禁用某个命令集\n" +
                $"选项:\n" +
                $"  -G              全局操作\n" +
                $"  -g <groupUin>   指定群\n" +
                $"  -h              打印帮助信息\n" +
                $"\n" +
                $"  enable      启用\n" +
                $"  disable     禁用";
            string reply = "";
            string? arg = args.FirstOrDefault();
            if (arg == null)
            {
                reply = "当前加载的命令集:\n";
                foreach (KeyValuePair<(CommandSet, BaseCommand), List<(Command handler, System.Reflection.MethodInfo method)>> set in commandManager.CmdSets)
                {
                    if (set.Key.Item1.Name == "核心功能")
                    {
                        continue;
                    }

                    string? name = set.Key.Item1.Name;
                    string? pakname = set.Key.Item1.PackageName;
                    reply +=
                        $"{(groupManager.IsCommandSetDisabled(messageEvent.GroupUin, pakname) ? "◇" : "◆")} {name}\n";
                }
                reply += $"\n被动模式: {(groupManager.IsPassiveMode(messageEvent.GroupUin) ? "开启" : "关闭")}\n" +
                    $"如需查看详细使用帮助, 请输入 /cmdctl -h";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            else
            {
                if (!PermissionManager.Instance.IsOperator(messageEvent.GroupUin, messageEvent.MemberUin) && !PermissionManager.Instance.IsAdmin(messageEvent.MemberUin))
                {
                    reply = $"你没有足够的权限来使用参数\n要求 {PermissionManager.Permission.Operator}.";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
            }
            List<string> sets = new();
            while (args.Count > 0)
            {
                arg = args.First();
                args.RemoveAt(0);

                switch (arg)
                {
                    case "-G":
                        {
                            if (!PermissionManager.Instance.IsAdmin(messageEvent.MemberUin))
                            {
                                reply = $"你没有足够的权限来使用这个参数: -G\n要求 {PermissionManager.Permission.Root}.";
                                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                                return;
                            }
                            global = true;
                            break;
                        }

                    case "enable": action = true; break;
                    case "disable": action = false; break;

                    case "-g":
                        {
                            string? group = args.FirstOrDefault();
                            if (args.Count > 0)
                            {
                                args.RemoveAt(0);
                            }

                            if (group == null)
                            {
                                reply = $"错误: 缺少参数: -g <groupUin>.";
                                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                                return;
                            }

                            if (!uint.TryParse(group, out groupUin))
                            {
                                reply = $"错误: 参数非法: \"{group}\" => -g <groupUin>.";
                                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                                return;
                            }

                            break;
                        }

                    case "-h":
                        {
                            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(help));
                            return;
                        }

                    default: sets.Add(arg); break;
                }
            }

            if (action == null)
            {
                reply = $"错误: 缺少参数: <enable/disable>.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            int count = 0;
            if (global)
            {
                foreach (string? set in sets)
                {
                    try
                    {
                        if (commandManager.ToggleCommandSet(set, (bool)action))
                        {
                            count++;
                        }
                    }
                    catch (Exception e)
                    {
                        reply = $"错误: {e.Message}";
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                        return;
                    }
                }
            }
            else
            {
                foreach (string? set in sets)
                {
                    try
                    {
                        if (groupManager.SetDisabledCommandSet(groupUin, (bool)action, set))
                        {
                            count++;
                        }
                    }
                    catch (Exception e)
                    {
                        reply = $"错误: {e.Message}";
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                        return;
                    }
                }
            }
            if (global)
            {
                reply = $"{count} 个命令集被全局 {((bool)action ? "启用" : "禁用")}.";
                Logger.Info($"G{messageEvent.GroupUin}|U{messageEvent.MemberUin} => {count} CommandSet(s) {((bool)action ? "Enabled" : "Disabled")} Globally.");
            }
            else
            {
                reply = $"G{groupUin} => {count} 个命令集被 {((bool)action ? "启用" : "禁用")}.";
                Logger.Info($"G{messageEvent.GroupUin}|U{messageEvent.MemberUin} => G{groupUin} => {count} CommandSet(s) {((bool)action ? "Enabled" : "Disabled")}.");
            }
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }

        [GroupMessageCommand("被动模式", @"^passive\s?([\s\S]+)?", PermissionManager.Permission.Operator)]
        public void OnPassiveMode(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            uint groupUin = messageEvent.GroupUin;
            while (args.Count > 0)
            {
                if (!PermissionManager.Instance.IsAdmin(messageEvent.MemberUin))
                {
                    reply = $"你没有足够的权限来使用这个参数: -G\n要求 {PermissionManager.Permission.Root}.";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
                string? arg = args.ElementAt(0);
                args.RemoveAt(0);

                if (uint.TryParse(arg, out uint value))
                {
                    groupUin = value;
                }
            }

            bool flag = groupManager.TogglePassiveMode(groupUin);

            reply = $"G{groupUin} => 被动模式 {(flag ? "启用" : "禁用")}.";
            Logger.Info($"G{messageEvent.GroupUin}|U{messageEvent.MemberUin} => G{groupUin} => Passive Mode {(flag ? "On" : "Off")}.");
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }

        [GroupMessageCommand("Ping", @"^ping", PermissionManager.Permission.Operator)]
        public void OnPing(Bot bot, GroupMessageEvent messageEvent)
        {
            long ticksNow = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            long ticksSend = messageEvent.EventTime.Ticks * 1000;

            DateTime test = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1970, 1, 1), TimeZoneInfo.Local).AddMilliseconds(ticksSend);
            string? reply = $"Pong! ({Math.Abs(ticksNow - ticksSend)}ms)\n" +
                $"Receive: {test}";

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }

        [GroupMessageCommand("命令集重载", @"^reload", PermissionManager.Permission.Root)]
        public void OnReload(Bot bot, GroupMessageEvent messageEvent)
        {
            commandManager.ReloadCommandSet();
            string? reply =
                "所有命令重载成功";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }

        [GroupMessageCommand("状态信息", @"^status")]
        public void OnStatus(Bot bot, GroupMessageEvent messageEvent)
        {
            PlatformID osVersion = Environment.OSVersion.Platform;
            int processorCount = Environment.ProcessorCount;
            string? clrVersion = Environment.Version.ToString();
            long usedMemoryMB = Environment.WorkingSet / 1024 / 1024;
            TimeSpan tickCount = DateTime.Now - Process.GetCurrentProcess().StartTime;

            CommandManager? cmdmgr = CommandManager.Instance;

            int groupCount = bot.GetGroupList().Result.Count;
            int friendCount = bot.GetFriendList().Result.Count;

            string? reply =
                $"[ProjektRin] {RinBuildStamp.Version} {RinBuildStamp.Branch}@{RinBuildStamp.CommitHash}\n" +
                $"当前系统平台: {osVersion} {processorCount} Thread(s)\n" +
                $"DotNET CLR版本: {clrVersion}\n" +
                $"内存占用: {usedMemoryMB} MB\n" +
                $"运行时间: {tickCount:dd\\d\\ hh\\h\\ mm\\m\\ ss\\s}\n\n" +

                $"[CMDMGR]\n" +
                $"载入了 {cmdmgr.CommandSetCount} 个命令集, {cmdmgr.CommandCount} 条命令.\n\n" +

                $"[KonataCore] {CoreBuildStamp.Version} {CoreBuildStamp.Branch}@{CoreBuildStamp.CommitHash}\n" +
                $"共有 {friendCount} 个好友, {groupCount} 个群.\n\n" +

                $"EOT\n{DateTime.Now:O}";

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }


    }
}
