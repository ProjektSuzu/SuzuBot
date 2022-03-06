using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using NLog;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.System;
using ProjektRin.Utils.BuildStamp;
using ProjektRin.Utils.Database.Tables;
using System.Diagnostics;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("核心功能")]
    internal class CoreCommand : BaseCommand
    {
        GroupManager groupManager;
        CommandManager commandManager;

        private static string TAG = "CORECMD";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public override string Help => "";

        public override void OnInit()
        {
            groupManager = GroupManager.Instance;
            commandManager = CommandManager.Instance;
        }
        public override void OnDisable() { }

        [GroupMessageCommand("帮助", new[] { @"^help\s?([\s\S]+)?", @"^帮助\s?([\s\S]+)?" })]
        public void OnHelp(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var reply = "";
            var setName = "";
            var sourceInfo = new SourceInfo(bot.Uin, bot.Name);
            var multiReply = MultiMsgChain.Create();
            if (args.Count > 0)
            {
                setName = args[0];
            }

            if (setName == "")
            {
                foreach (var set in commandManager.CmdSets)
                {
                    if (set.Key.Item1.Name == "核心功能") continue;
                    var help = set.Key.Item2.Help;
                    var message = new MessageBuilder(help);
                    multiReply.AddMessage(sourceInfo, message);
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
                var set = commandManager.CmdSets.Where(x => x.Key.Item1.Name == setName).First();
                var help = set.Key.Item2.Help;
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(help));
                return;
            }
        }

        [GroupMessageCommand("用户信息", new[] { @"^info\s?([\s\S]+)?", @"^信息\s?([\s\S]+)?" })]
        public void OnUserInfo(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var reply = "";
            var uin = messageEvent.MemberUin;
            var create = true;
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
            var info = UserInfoManager.GetUserInfo(uin, create);
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

        [GroupMessageCommand("命令集控制", @"^cmdctl\s?([\s\S]+)?", PermissionManager.Permission.Operator)]
        public void OnCommandControl(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var groupUin = messageEvent.GroupUin;
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
            var arg = args.FirstOrDefault();
            if (arg == null)
            {
                reply = "当前加载的命令集:\n";
                foreach (var set in commandManager.CmdSets)
                {
                    if (set.Key.Item1.Name == "核心功能") continue;
                    var name = set.Key.Item1.Name;
                    reply +=
                        $"{(groupManager.IsCommandSetDisabled(messageEvent.GroupUin, name) ? "◇" : "◆")} {name}\n";
                }
                reply += $"\n被动模式: {(groupManager.IsPassiveMode(messageEvent.GroupUin) ? "开启" : "关闭")}\n" +
                    $"如需查看详细使用帮助, 请输入 /cmdctl -h";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
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
                                reply = $"你没有足够的权限来使用这个参数: -G\n要求 {PermissionManager.Permission.Admin}.";
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
                            var group = args.FirstOrDefault();
                            if (args.Count > 0) args.RemoveAt(0);
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

            var count = 0;
            if (global)
            {
                foreach (var set in sets)
                {
                    try
                    {
                        if (commandManager.ToggleCommandSet(set, (bool)action)) count++;
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
                foreach (var set in sets)
                {
                    try
                    {
                        if (groupManager.SetDisabledCommandSet(groupUin, (bool)action, set)) count++;
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
            var reply = "";
            var groupUin = messageEvent.GroupUin;
            while (args.Count > 0)
            {
                if (!PermissionManager.Instance.IsAdmin(messageEvent.MemberUin))
                {
                    reply = $"你没有足够的权限来使用这个参数: -G\n要求 {PermissionManager.Permission.Admin}.";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
                var arg = args.ElementAt(0);
                args.RemoveAt(0);

                if (uint.TryParse(arg, out var value))
                {
                    groupUin = value;
                }
            }

            var flag = groupManager.TogglePassiveMode(groupUin);

            reply = $"G{groupUin} => 被动模式 {(flag ? "启用" : "禁用")}.";
            Logger.Info($"G{messageEvent.GroupUin}|U{messageEvent.MemberUin} => G{groupUin} => Passive Mode {(flag ? "On" : "Off")}.");
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }

        [GroupMessageCommand("Ping", @"^ping", PermissionManager.Permission.Operator)]
        public void OnPing(Bot bot, GroupMessageEvent messageEvent)
        {
            var ticksNow = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            long ticksSend = (long)messageEvent.MessageTime * 1000;

            var test = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1970, 1, 1), TimeZoneInfo.Local).AddMilliseconds(ticksSend);
            var reply = $"Pong! ({Math.Abs(ticksNow - ticksSend)}ms)\n" +
                $"Receive: {test}";

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }

        [GroupMessageCommand("命令集重载", @"^reload", PermissionManager.Permission.Admin)]
        public void OnReload(Bot bot, GroupMessageEvent messageEvent)
        {
            commandManager.ReloadCommandSet();
            var reply =
                "所有命令重载成功";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }

        [GroupMessageCommand("状态信息", @"^status")]
        public void OnStatus(Bot bot, GroupMessageEvent messageEvent)
        {
            var osVersion = Environment.OSVersion.Platform;
            var processorCount = Environment.ProcessorCount;
            var clrVersion = Environment.Version.ToString();
            var usedMemoryMB = Environment.WorkingSet / 1024 / 1024;
            var tickCount = DateTime.Now - Process.GetCurrentProcess().StartTime;

            var cmdmgr = CommandManager.Instance;

            var groupCount = bot.GetGroupList().Result.Count;
            var friendCount = bot.GetFriendList().Result.Count;

            var reply =
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
