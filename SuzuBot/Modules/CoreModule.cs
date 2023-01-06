using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Newtonsoft.Json;
using SuzuBot.Common;
using SuzuBot.Common.Attributes;
using SuzuBot.Common.EventArgs.Messages;
using SuzuBot.Utils;

namespace SuzuBot.Modules;

[Module("核心模块", IsCritical = true)]
internal class CoreModule : BaseModule
{
    string[] _pingReplys = Array.Empty<string>();
    public const uint RinBotGroup = 955578812U;

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
    [Command("Echo", "echo", MatchType = Common.Attributes.MatchType.StartsWith, Priority = 0)]
    public Task Echo(MessageEventArgs eventArgs, string[] args)
    {
        if (!args.Any()) return Task.CompletedTask;
        else return eventArgs.SendMessage(MessageBuilder.Eval(string.Join(' ', args)));
    }
    [Command("反馈", "report", MatchType = Common.Attributes.MatchType.StartsWith, Priority = 0)]
    public async Task Report(MessageEventArgs eventArgs, string[] args)
    {
        MessageBuilder builder = new MessageBuilder("[Report]\n")
            .Text($"来自用户 {eventArgs.SenderName}({eventArgs.SenderId})|{eventArgs.ReceiverName}({eventArgs.ReceiverId}) 的反馈\n")
            ;
        MessageBuilder evalBuilder = MessageBuilder.Eval(string.Join(' ', args));
        await eventArgs.Bot.SendGroupMessage(RinBotGroup, builder + evalBuilder);
        builder = new MessageBuilder("[Report]\n已收到你的反馈\n(〃^ω^) 感谢你帮助 SuzuBot 变得更好");
        await eventArgs.Reply(builder);
    }
    [Command("帮助", "help", Priority = 0)]
    public Task Help(MessageEventArgs eventArgs)
    {
        var builder = new MessageBuilder("[Help]\n")
            .Text($"SuzuBot-{SuzuBotBuildStamp.Version}\n")
            .Text("AkulaKirov 2018 GPL-3.0\n")
            .Text(@"帮助文档请查阅: https://suzubot.akulak.icu/");
        return eventArgs.Reply(builder);
    }
    [Command("信息", "info", Priority = 0)]
    public async Task Info(MessageEventArgs eventArgs)
    {
        var info = await Context.DataBaseManager.GetUserInfo(eventArgs.SenderId);
        var builder = new MessageBuilder("[Info]\n")
            .Text($"{eventArgs.SenderName}\n")
            .Text($"SC: {info.Coin}\n")
            .Text($"好感: {info.Favor}\n")
            .Text($"权限组: {info.AuthGroup}\n");
        eventArgs.Reply(builder);
    }
    [Command("退群", "quit", SourceType = SourceType.Group, Priority = 0, AuthGroup = "operator", AuthFailWarning = true)]
    public Task Quit(GroupMessageEventArgs eventArgs)
    {
        return eventArgs.Bot.GroupLeave(eventArgs.ReceiverId);
    }
}
