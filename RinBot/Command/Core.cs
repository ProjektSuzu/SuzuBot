using Konata.Core;
using Konata.Core.Interfaces.Api;
using RinBot.BuildStamp;
using RinBot.Core.Component.Command;
using RinBot.Core.Component.Command.CustomAttribute;
using RinBot.Core.Component.Event;
using RinBot.Core.Component.Permission;
using RinBot.Core.KonataCore;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace RinBot.Command
{
    [Module("Core", "org.akulak.core", critical: true)]
    internal class Core
    {
        [Command("帮助", "help", MatchingType.StartsWith, ReplyType.Reply)]
        public string OnHelp(RinEvent e)
        {
            return $"[RinBot] {RinBotBuildStamp.Version}\n请访问 https://docs-rinbot.akulak.icu/modules/ 来获取帮助信息";
        }

        [Command("退群", "quit", MatchingType.StartsWith, ReplyType.Reply, UserRole.Operator)]
        public void OnQuit(RinEvent e)
        {
            if (e.EventSourceType == EventSourceType.QQ)
            {
                if (e.EventSubjectType == EventSubjectType.DirectMessage)
                    return;
                if (e.EventSubjectType == EventSubjectType.Group)
                {
                    var bot = e.OriginalSender as Bot;
                    bot.GroupLeave(uint.Parse(e.SubjectId));
                }
            }
        }

        [Command("模块管理", "cmdctl", MatchingType.StartsWith, ReplyType.Reply, UserRole.Operator)]
        public string OnModuleManage(RinEvent e, List<string> args)
        {
            var cmdlist = CommandManager.Instance.GetModuleInfos();
            List<string> disabled = new();
            if (e.EventSourceType == EventSourceType.QQ)
            {
                if (e.EventSubjectType == EventSubjectType.Group)
                {
                    var groupId = uint.Parse(e.SubjectId);
                    var info = PermissionManager.Instance.GetQQGroupInfo(groupId);

                    disabled = info.DisableModuleIds;
                }
            }

            if (args.Count == 0)
            {
                StringBuilder builder = new();
                builder.AppendLine("[CMDCTL]");
                if (e.EventSubjectType == EventSubjectType.Group)
                {
                    foreach (var cmd in cmdlist)
                    {
                        if (!cmd.IsEnable)
                            builder.Append("⬛");
                        else if (cmd.DefaultEnableType == ModuleEnableConfig.NormallyEnable && disabled.Any(x => x == cmd.ModuleID))
                            builder.Append("🔷");
                        else if (cmd.DefaultEnableType == ModuleEnableConfig.NormallyDisable && disabled.All(x => x != cmd.ModuleID))
                            builder.Append("🔷");
                        else
                            builder.Append("⚪");
                        builder.Append($" {cmd.ModuleName}\n");
                    }

                    builder.AppendLine();
                    builder.AppendLine("⬛ 全局关闭");
                    builder.AppendLine("🔷 当前群聊关闭");
                    builder.AppendLine("⚪ 开启");
                }
                else
                {
                    foreach (var cmd in cmdlist)
                    {
                        if (!cmd.IsEnable)
                            builder.Append("⬛");
                        else
                            builder.Append("⚪");
                        builder.Append($" {cmd.ModuleName}\n");
                    }

                    builder.AppendLine();
                    builder.AppendLine("⬛ 全局关闭");
                    builder.AppendLine("⚪ 开启");
                }
                return builder.ToString();
            }
            else
            {
                if (e.EventSubjectType != EventSubjectType.Group) return "[CMDCTL]\n仅允许在群聊环境内执行此操作";

                List<string> moduleNames = new();
                var targetGroupID = e.SubjectId;
                bool? action = null;
                bool global = false;

                Queue<string> queue = new Queue<string>(args);

                while (queue.Count > 0)
                {
                    var arg = queue.Dequeue();
                    switch (arg)
                    {
                        case "enable":
                        case "开启":
                            action = true;
                            break;

                        case "disable":
                        case "关闭":
                            action = false;
                            break;

                        //case "-t":
                        //case "--target":
                        //    {
                        //        if (queue.Count <= 0)
                        //            return "[CMDCTL]\n缺少参数 <groupID>";
                        //        else
                        //        {
                        //            targetGroupID = queue.Dequeue();
                        //            break;
                        //        }
                        //    }

                        default:
                            moduleNames.Add(arg);
                            break;
                    }
                }

                if (action == null) return "[CMDCTL]\n未指定操作";
                List<ModuleInfo> modules = new();
                foreach (var name in moduleNames)
                {
                    var module = cmdlist.FirstOrDefault(x => x.ModuleName == name || x.ModuleID == name);
                    if (module == null)
                        return $"[CMDCTL]\n未找到模块 {name}";
                    else
                        modules.Add(module);
                }

                var groupId = uint.Parse(e.SubjectId);
                var info = PermissionManager.Instance.GetQQGroupInfo(groupId);
                foreach (var module in modules)
                {
                    if ((bool)action)
                    {
                        if (module.DefaultEnableType == ModuleEnableConfig.NormallyEnable || module.DefaultEnableType == ModuleEnableConfig.WhiteListOnly)
                        {
                            if (disabled.Any(x => x == module.ModuleID))
                                disabled.Remove(module.ModuleID);
                        }
                        else
                        {
                            if (disabled.All(x => x != module.ModuleID))
                                disabled.Add(module.ModuleID);
                        }
                    }
                    else
                    {
                        if (module.DefaultEnableType == ModuleEnableConfig.NormallyEnable || module.DefaultEnableType == ModuleEnableConfig.WhiteListOnly)
                        {
                            if (disabled.All(x => x != module.ModuleID))
                                disabled.Add(module.ModuleID);
                        }
                        else
                        {
                            if (disabled.Any(x => x == module.ModuleID))
                                disabled.Remove(module.ModuleID);
                        }
                    }
                }
                info.DisableModuleIds = disabled;
                PermissionManager.Instance.UpdateQQGroupInfo(info);
                return $"[CMDCTL]\n已{((bool)action ? "开启" : "关闭")} {modules.Count} 个模块.";
            }
        }

        [Command("Ping", "ping", MatchingType.StartsWith, ReplyType.Reply)]
        public string OnPing(RinEvent e)
        {
            return "Don`t\nCry\nI`m just a\nBot(ᗜˬᗜ)";
        }

        [Command("用户信息", new[] { "info", "信息" }, (int)MatchingType.Exact, ReplyType.Reply)]
        public string OnInfo(RinEvent e)
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("[UserInfo]");
            if (e.EventSourceType == EventSourceType.QQ)
            {
                var info = PermissionManager.Instance.GetQQUserInfo(uint.Parse(e.SenderId));
                stringBuilder.AppendLine($"用户Id: {info.UserId}");
                stringBuilder.AppendLine($"权限: {info.UserRole}");
                stringBuilder.AppendLine($"经验: {info.Exp} exp");
                stringBuilder.AppendLine($"内存: {info.MemoryStr}");

            }
            else if (e.EventSourceType == EventSourceType.Telegram)
            {

            }
            return stringBuilder.ToString();
        }

        [Command("状态汇报", "status", (int)MatchingType.StartsWith, ReplyType.Reply)]
        public string OnStatus(RinEvent e)
        {
            int processorCount = Environment.ProcessorCount;
            long usedMemoryMB = Environment.WorkingSet / 1024 / 1024;
            TimeSpan tickCount = DateTime.Now - Process.GetCurrentProcess().StartTime;

            int groupCount = KonataBot.Instance.Bot.GetGroupList(true).Result.Count;
            int friendCount = KonataBot.Instance.Bot.GetFriendList(true).Result.Count;
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine($"[RinBot]{RinBotBuildStamp.Version}");
            stringBuilder.AppendLine($"{RinBotBuildStamp.CommitHash.Substring(RinBotBuildStamp.CommitHash.Length - 8)}@{RinBotBuildStamp.Branch}");
            stringBuilder.AppendLine($"当前系统平台: {RuntimeInformation.RuntimeIdentifier} {processorCount} Thread(s).");
            stringBuilder.AppendLine($"DotNET 版本: {RuntimeInformation.FrameworkDescription}.");
            stringBuilder.AppendLine($"内存占用: {usedMemoryMB} MB.");
            stringBuilder.AppendLine($"运行时间: {tickCount:dd\\d\\ hh\\h\\ mm\\m\\ ss\\s}.");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"[CMD]");
            stringBuilder.AppendLine($"载入了 {CommandManager.Instance.ModuleCount} 个模块, {CommandManager.Instance.CommandCount} 个命令.");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"[Konata.Core]{CoreBuildStamp.Version}");
            stringBuilder.AppendLine($"{CoreBuildStamp.CommitHash.Substring(CoreBuildStamp.CommitHash.Length - 8)}@{CoreBuildStamp.Branch}");
            stringBuilder.AppendLine($"共有 {friendCount} 个好友, {groupCount} 个群.");
            stringBuilder.AppendLine($"{DateTime.Now:O}");
            stringBuilder.AppendLine("EOT");

            return stringBuilder.ToString();
        }
    }
}
