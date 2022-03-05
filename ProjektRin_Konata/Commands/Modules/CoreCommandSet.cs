using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using NLog;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.System;
using ProjektRin.Utils.BuildStamp;
using System.Diagnostics;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("CoreCommands")]
    internal class CoreCommandSet : BaseCommand
    {
        GroupManager groupManager;
        CommandManager commandManager;

        private static string TAG = "CORECMD";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public override void OnInit()
        {
            groupManager = GroupManager.Instance;
            commandManager = CommandManager.Instance;
        }
        public override void OnDisable() { }

        [GroupMessageCommand("CommandControl", @"^cmdctl\s?([\s\S]+)?")]
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
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(help));
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

        [GroupMessageCommand("PassiveMode", @"^passive\s?([\s\S]+)?")]
        public void OnPassiveMode(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var groupUin = messageEvent.GroupUin;
            while (args.Count > 0)
            {
                var arg = args.ElementAt(0);
                args.RemoveAt(0);

                if (uint.TryParse(arg, out var value))
                {
                    groupUin = value;
                }
            }

            var flag = groupManager.TogglePassiveMode(groupUin);

            var reply = $"G{groupUin} => 被动模式 {(flag ? "启用" : "禁用")}.";
            Logger.Info($"G{messageEvent.GroupUin}|U{messageEvent.MemberUin} => G{groupUin} => Passive Mode {(flag ? "On" : "Off")}.");
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }

        [GroupMessageCommand("Ping", @"^ping")]
        public void OnPing(Bot bot, GroupMessageEvent messageEvent)
        {
            var ticksNow = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            long ticksSend = (long)messageEvent.MessageTime * 1000;

            var test = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1970, 1, 1), TimeZoneInfo.Local).AddMilliseconds(ticksSend);
            var reply = $"Pong! ({Math.Abs(ticksNow - ticksSend)}ms)\n" +
                $"Receive: {test}";

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }

        [GroupMessageCommand("Reload", @"^reload")]
        public void OnReload(Bot bot, GroupMessageEvent messageEvent)
        {
            commandManager.ReloadCommandSet();
            var reply =
                "所有命令重载成功";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }

        [GroupMessageCommand("Status", @"^status")]
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
