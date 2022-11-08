using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Newtonsoft.Json;
using RinBot.Common;
using RinBot.Common.Attributes;
using RinBot.Common.EventArgs.Messages;
using RinBot.Utils;

namespace RinBot.Modules;

[Module("核心模块", IsCritical = true)]
internal class CoreModule : BaseModule
{
    string[] _pingReplys = Array.Empty<string>();

    public override void Init()
    {
        base.Init();
        _pingReplys = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(Path.Combine(ResourceDirPath, "pingReplys.json")))!;
    }

    [Command("Ping", "ping", Priority = 0)]
    public Task PingReply(MessageEventArgs eventArgs)
    {
        return eventArgs.Reply(_pingReplys[new Random().Next(_pingReplys.Length)]);
    }
    [Command("状态汇报", "status", Priority = 0)]
    public Task BotStatusReport(MessageEventArgs eventArgs)
    {
        StringBuilder builder = new("[RinBot]\n");
        builder.AppendLine($"RinBot-{RinBotBuildStamp.Version}");
        builder.AppendLine($"Branch: {RinBotBuildStamp.Branch}@{RinBotBuildStamp.CommitHash[..8]}");
        builder.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
        builder.AppendLine($"Architecture: {RuntimeInformation.RuntimeIdentifier} {Environment.ProcessorCount} Thread(s)");
        builder.AppendLine($"Memory: {Environment.WorkingSet / 1000000} MB");
        builder.AppendLine($"UpTime: {DateTime.Now - Process.GetCurrentProcess().StartTime:dd\\d\\ hh\\h\\ mm\\m\\ ss\\s}");
        builder.AppendLine();
        builder.AppendLine($"Modules/Commands: {Context.ModuleManager.Modules.Count}/{Context.ModuleManager.Commands.Count}");
        builder.AppendLine($"ExecuteCount: {Context.ModuleManager.ExecuteCount}");
        builder.AppendLine($"ExceptionCount: {Context.ModuleManager.ExceptionCount}");
        if (Context.ModuleManager.LastException is not null)
        {
            builder.AppendLine($"LastException: {Context.ModuleManager.LastException.GetType().Name}");
            builder.AppendLine(Context.ModuleManager.LastException.Message);
        }
        builder.AppendLine();
        builder.AppendLine("[Konata.Core]");
        builder.AppendLine($"Konata.Core-{KonataBuildStamp.Version}");
        builder.AppendLine($"Branch: {KonataBuildStamp.Branch}@{KonataBuildStamp.CommitHash[..8]}");
        builder.AppendLine($"Groups/Friends: {eventArgs.Bot.GetGroupList().Result.Count}/{eventArgs.Bot.GetFriendList().Result.Count}");
        builder.AppendLine(DateTime.UtcNow.ToString("O"));

        return eventArgs.Reply(builder.ToString());
    }
    [Command("Echo", "echo", MatchType = Common.Attributes.MatchType.StartsWith, Priority = 0)]
    public Task Echo(MessageEventArgs eventArgs, string[] args)
    {
        if (!args.Any()) return Task.CompletedTask;
        else return eventArgs.SendMessage(MessageBuilder.Eval(string.Join(' ', args)));
    }
}
