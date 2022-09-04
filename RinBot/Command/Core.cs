using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Newtonsoft.Json;
using RinBot.BuildStamp;
using RinBot.Core;
using RinBot.Core.Components;
using RinBot.Core.Components.Attributes;
using RinBot.Core.Components.Commands;
using RinBot.Core.Components.Databases.Tables;
using RinBot.Core.Components.Managers;
using RinBot.Core.KonataCore.Contacts.Models;
using RinBot.Core.KonataCore.Events;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RinBot.Command
{
    [Module("核心功能", "AkulaKirov.Core", true)]
    internal class Core
    {
        private static readonly string RESOURCE_DIR_PATH = Path.Combine(GlobalScope.RESOURCE_DIR_PATH, "AkulaKirov.Core");
        private static readonly string PING_REPLY_PATH = Path.Combine(RESOURCE_DIR_PATH, "pingReplys.json");
        public Core()
        {
            Directory.CreateDirectory(RESOURCE_DIR_PATH);
            if (!File.Exists(PING_REPLY_PATH))
            {
                pingReplys = Array.Empty<string>();
            }
            else
            {
                pingReplys = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(PING_REPLY_PATH))
                             ?? Array.Empty<string>();
            }
        }

        private readonly string[] pingReplys;

        [TextCommand("帮助", new[] { "help" , "帮助" })]
        public void OnHelp(MessageEventArgs messageEvent)
        {
            messageEvent.Reply($"[RinBot] {RinBotBuildStamp.Version}\n请访问 https://docs-rinbot.akulak.icu/modules/ 来获取帮助信息");
        }

        [TextCommand("Ping", "ping")]
        public void OnPing(MessageEventArgs messageEvent)
        {
            var chains = MessageBuilder.Eval(pingReplys[new Random().Next(pingReplys.Length)]);
            messageEvent.Reply(chains);
        }

        [TextCommand("退群", new[] { "quit", "退群" })]
        public void OnQuit(MessageEventArgs messageEvent)
        {
            if (messageEvent.Subject is Group)
            {
                GlobalScope.KonataBot.Bot.GroupLeave(messageEvent.Subject.Uin);
            }
            else
            {
                messageEvent.Reply("该对话场景不支持此操作");
            }
        }

        [TextCommand("查看信息", "info")]
        public void OnInfo(MessageEventArgs messageEvent)
        {
            QQUserInfo info = GlobalScope.PermissionManager.GetUserInfo(messageEvent.Sender.Uin);
            messageEvent.Reply("[Info]\n" +
                $"{messageEvent.Sender.Name}\n" +
                $"用户组: {GlobalScope.PermissionManager.GetUserLevel(messageEvent.Sender)}\n" +
                $"当前拥有 {info.Coin} RC\n" +
                $"当前好感度为 {info.Favor}\n");
        }

        [TextCommand("状态汇报", "status")]
        public void OnStatus(MessageEventArgs messageEvent)
        {
            int processorCount = Environment.ProcessorCount;
            long usedMemoryMB = Environment.WorkingSet / 1024 / 1024;
            TimeSpan tickCount = DateTime.Now - Process.GetCurrentProcess().StartTime;

            int groupCount = GlobalScope.KonataBot.Bot.GetGroupList(true).Result.Count;
            int friendCount = GlobalScope.KonataBot.Bot.GetFriendList(true).Result.Count;
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine($"[RinBot]{RinBotBuildStamp.Version}");
            stringBuilder.AppendLine($"{RinBotBuildStamp.CommitHash[^8..]}@{RinBotBuildStamp.Branch}");
            stringBuilder.AppendLine($"当前系统平台: {RuntimeInformation.RuntimeIdentifier} {processorCount} Thread(s).");
            stringBuilder.AppendLine($"DotNET 版本: {RuntimeInformation.FrameworkDescription}.");
            stringBuilder.AppendLine($"内存占用: {usedMemoryMB} MB.");
            stringBuilder.AppendLine($"运行时间: {tickCount:dd\\d\\ hh\\h\\ mm\\m\\ ss\\s}.");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"[CMD]");
            stringBuilder.AppendLine($"载入了 {GlobalScope.CommandManager.ModuleCount} 个模块, {GlobalScope.CommandManager.CommandCount} 个命令.");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"[Konata.Core]{CoreBuildStamp.Version}");
            stringBuilder.AppendLine($"{CoreBuildStamp.CommitHash[^8..]}@{CoreBuildStamp.Branch}");
            stringBuilder.AppendLine($"共有 {friendCount} 个好友, {groupCount} 个群.");
            stringBuilder.AppendLine($"{DateTime.Now:O}");
            stringBuilder.AppendLine("EOT");

            messageEvent.Reply(stringBuilder.ToString());
        }

        [TextCommand("模块管理", "cmdctl")]
        public void OnCommandControl(MessageEventArgs messageEvent, CommandStruct command)
        {
            var moduleList = GlobalScope.CommandManager.ModuleTable
                .Select(x => x.Value);
            StringBuilder builder = new();
            builder.AppendLine("[CMDCTL]");
            if (command.FuncArgs.Length <= 0)
            {
                string[] groupModuleIds;
                bool whiteListed = false;
                if (messageEvent.Subject is Group group)
                {
                    var groupInfo = GlobalScope.PermissionManager.GetGroupInfo(group.Uin);
                    whiteListed = GlobalScope.PermissionManager.IsGroupInWhiteList(group.Uin).Result;
                    groupModuleIds = groupInfo.ModuleIds.ToArray();
                }
                else
                {
                    groupModuleIds = Array.Empty<string>();
                }


                foreach (var module in moduleList)
                {
                    if (module.EnableType == ModuleEnableType.WhiteListOnly && !whiteListed)
                        continue;
                    if (!module.IsEnabled)
                        builder.Append('⬛');
                    else if (module.EnableType == ModuleEnableType.NormallyEnabled && groupModuleIds.Contains(module.ModuleId))
                        builder.Append("🔷");
                    else if (module.EnableType == ModuleEnableType.NormallyDisabled && !groupModuleIds.Contains(module.ModuleId))
                        builder.Append("🔷");
                    else
                        builder.Append('⚪');

                    builder.Append($" {module.Name}\n");
                }
                builder.AppendLine();
                builder.AppendLine("⬛ 全局关闭");
                builder.AppendLine("🔷 当前群聊关闭");
                builder.AppendLine("⚪ 开启");

                messageEvent.Reply(builder.ToString());
            }
            else
            {
                if (messageEvent.Subject is not Group)
                {
                    builder.AppendLine("该对话场景不支持此操作");
                    messageEvent.Reply(builder.ToString());
                }
                var level = GlobalScope.PermissionManager.GetUserLevel(messageEvent.Sender);
                if (level < UserPermission.GroupAdmin)
                {
                    builder.AppendLine("你没有执行该操作的权限\n" +
                        $"要求 {UserPermission.GroupAdmin}\n" +
                        $"你的权限级别为 {level}");
                    messageEvent.Reply(builder.ToString());
                    return;
                }
                var groupInfo = GlobalScope.PermissionManager.GetGroupInfo(messageEvent.Subject.Uin);
                bool action = false;
                List<string> operateModuleIds = new();
                if (command.FuncArgs[0] == "enable")
                {
                    action = true;
                }
                else if (command.FuncArgs[0] == "disable")
                {
                    action = false;
                }
                else
                {
                    builder.AppendLine($"参数错误: {command.FuncArgs[0]} => <enable/disable>");
                    messageEvent.Reply(builder.ToString());
                }

                if (command.FuncArgs.Length >= 2)
                {
                    operateModuleIds = command.FuncArgs[1..].ToList();
                }

                var groupModules = groupInfo.ModuleIds.ToList();

                foreach (var moduleName in operateModuleIds)
                {
                    var moduleId = GlobalScope.CommandManager.GetModuleInfo(moduleName)?.ModuleId
                                 ?? GlobalScope.CommandManager.GetModuleInfoByName(moduleName)?.ModuleId;
                    var module = GlobalScope.CommandManager.ModuleTable[moduleId];

                    if (module == null)
                    {
                        builder.AppendLine($"找不到模块: {moduleId}");
                    }
                    else if (module.IsCritical)
                    {
                        builder.AppendLine($"不允许操作重要模块: {module.Name}");
                    }
                    else
                    {

                        if (action)
                        {
                            if (module.EnableType == ModuleEnableType.NormallyDisabled)
                            {
                                if (!groupModules.Contains(moduleId))
                                    groupModules.Add(moduleId);
                            }
                            else
                            {
                                if (groupModules.Contains(moduleId))
                                    groupModules.Remove(moduleId);
                            }
                        }
                        else
                        {
                            if (module.EnableType == ModuleEnableType.NormallyDisabled)
                            {
                                if (groupModules.Contains(moduleId))
                                    groupModules.Remove(moduleId);
                            }
                            else
                            {
                                if (!groupModules.Contains(moduleId))
                                    groupModules.Add(moduleId);
                            }
                        }
                        builder.AppendLine($"模块 {module.Name} {(action ? "已开启" : "已关闭")}");
                    }
                }
                groupInfo.ModuleIds = groupModules;
                GlobalScope.PermissionManager.UpdateGroupInfo(groupInfo);
                messageEvent.Reply(builder.ToString());
            }
        }
    }
}
