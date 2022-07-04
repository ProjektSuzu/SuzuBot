using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using RinBot.BuildStamp;
using RinBot.Core.Component.Command;
using RinBot.Core.Component.Command.CustomAttribute;
using RinBot.Core.Component.Event;
using RinBot.Core.Component.Message;
using RinBot.Core.Component.Message.Model;
using RinBot.Core.Component.Permission;
using RinBot.Core.KonataCore;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RinBot.Command
{
    [Module("Core", "org.akulak.core")]
    internal class Core
    {
        [Command("帮助", "help", MatchingType.Exact, ReplyType.Reply)]
        public string OnHelp(RinEvent e)
        {
            return $"[RinBot] {RinBotBuildStamp.Version}\n请访问 https://docs-rinbot.akulak.icu 来获取帮助信息";
        }

        [Command("模块重载", "reload", MatchingType.Exact, ReplyType.Reply, UserRole.Admin)]
        public string OnReload(RinEvent e)
        {
            CommandManager.Instance.ClearCommands();
            CommandManager.Instance.RegisterCommands();
            return $"[CMD]\n载入了 {CommandManager.Instance.ModuleCount} 个模块, {CommandManager.Instance.CommandCount} 个命令.";
        }

        [Command("Ping", "ping", MatchingType.Exact, ReplyType.Reply)]
        public string OnPing(RinEvent e)
        {
            return "Pong!";
        }

        [Command("状态汇报", "status", (int)MatchingType.Exact, ReplyType.Reply)]
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
