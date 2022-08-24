using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Newtonsoft.Json;
using RinBot.BuildStamp;
using RinBot.Core;
using RinBot.Core.Components;
using RinBot.Core.Components.Attributes;
using RinBot.Core.Components.Commands;
using RinBot.Core.Components.Managers;
using RinBot.Core.KonataCore.Contacts.Models;
using RinBot.Core.KonataCore.Events;
using System.Diagnostics;
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

        [TextCommand("帮助", "help")]
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

        [TextCommand("退群", "quit")]
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
            stringBuilder.AppendLine($"{RinBotBuildStamp.CommitHash.Substring(RinBotBuildStamp.CommitHash.Length - 8)}@{RinBotBuildStamp.Branch}");
            stringBuilder.AppendLine($"当前系统平台: {RuntimeInformation.RuntimeIdentifier} {processorCount} Thread(s).");
            stringBuilder.AppendLine($"DotNET 版本: {RuntimeInformation.FrameworkDescription}.");
            stringBuilder.AppendLine($"内存占用: {usedMemoryMB} MB.");
            stringBuilder.AppendLine($"运行时间: {tickCount:dd\\d\\ hh\\h\\ mm\\m\\ ss\\s}.");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"[CMD]");
            stringBuilder.AppendLine($"载入了 {GlobalScope.CommandManager.ModuleCount} 个模块, {GlobalScope.CommandManager.CommandCount} 个命令.");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"[Konata.Core]{CoreBuildStamp.Version}");
            stringBuilder.AppendLine($"{CoreBuildStamp.CommitHash.Substring(CoreBuildStamp.CommitHash.Length - 8)}@{CoreBuildStamp.Branch}");
            stringBuilder.AppendLine($"共有 {friendCount} 个好友, {groupCount} 个群.");
            stringBuilder.AppendLine("EOT");
            stringBuilder.AppendLine($"{DateTime.Now:O}");

            messageEvent.Reply(stringBuilder.ToString());
        }

        [TextCommand("模块管理", "cmdctl", UserPermission.GroupAdmin)]
        public void OnCommandControl(MessageEventArgs messageEvent, CommandStruct command)
        {
            var moduleList = GlobalScope.CommandManager.ModuleTable
                .Select(x => x.Value);
            StringBuilder builder = new();
            builder.AppendLine("[CMDCTL]");
            if (command.FuncArgs.Length <= 0)
            {
                string[] groupModuleIds;
                if (messageEvent.Subject is Group group)
                {
                    var groupInfo = GlobalScope.PermissionManager.GetGroupInfo(group.Uin);
                    groupModuleIds = groupInfo.ModuleIds.ToArray();
                }
                else
                {
                    groupModuleIds = Array.Empty<string>();
                }

                foreach (var module in moduleList)
                {
                    if (!module.IsEnabled)
                        builder.Append("⬛");
                    else if (module.EnableType == ModuleEnableType.NormallyEnabled && groupModuleIds.Contains(module.ModuleId))
                        builder.Append("🔷");
                    else if (module.EnableType == ModuleEnableType.NormallyDisabled && !groupModuleIds.Contains(module.ModuleId))
                        builder.Append("🔷");
                    else
                        builder.Append("⚪");

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

                foreach (var moduleId in operateModuleIds)
                {
                    var module = GlobalScope.CommandManager.GetModuleInfo(moduleId)
                                 ?? GlobalScope.CommandManager.GetModuleInfoByName(moduleId);
                    if (module == null)
                    {
                        builder.AppendLine($"找不到模块: {moduleId}");
                    }
                    else if (module.IsCritical)
                    {
                        builder.AppendLine($"不允许操作重要模块: {moduleId}");
                    }
                    else
                    {
                        if (groupInfo.ModuleIds.Contains(moduleId))
                        {
                            groupInfo.ModuleIds.Remove(moduleId);
                        }
                        else
                        {
                            groupInfo.ModuleIds.Add(moduleId);
                        }
                        builder.AppendLine($"模块 {module.Name} 操作成功");
                    }
                }
                GlobalScope.PermissionManager.UpdateGroupInfo(groupInfo);
                messageEvent.Reply(builder.ToString());
            }
        }

        [TextCommand("模块重载", "reload", UserPermission.Root)]
        public void OnReload(MessageEventArgs messageEvent)
        {
            GlobalScope.CommandManager.ReloadModules();
            messageEvent.Reply($"载入了 {CommandManager.Instance.ModuleCount} 个模块, {CommandManager.Instance.CommandCount} 个命令.");
        }

        [TextCommand("测试", "test")]
        public void OnTest(MessageEventArgs messageEvent)
        {
            var builder = new MessageBuilder();
            var datetime = Utils.GetUnixDateTimeMilliseconds(1590927077539);
            builder.Text(datetime.ToString());
            messageEvent.Reply(builder);
        }

    }
}
