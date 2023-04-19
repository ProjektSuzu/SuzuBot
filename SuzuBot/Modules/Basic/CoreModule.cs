using System.Text;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;
using SuzuBot.Core.Tables;
using SuzuBot.Utils;

namespace SuzuBot.Modules.Basic;

public class CoreModule : BaseModule
{
    private string[] _pingReplies;
    private Random _random = new Random();

    public const uint RinBotGroup = 955578812U;

    public CoreModule()
    {
        Name = "核心模块";
        IsCritical = true;
    }

    public override bool Init()
    {
        if (!base.Init()) return false;
        _pingReplies = File.ReadAllText(Path.Combine(ResourceDirPath, "pingReplies.json"))
            .DeserializeJson<string[]>() ?? new[] { "Pong!" };
        return true;
    }

    [Command("Ping", "^ping$", Priority = 0)]
    public Task Ping(MessageEventArgs eventArgs, string[] args)
    {
        return eventArgs.Reply(new MessageBuilder(_pingReplies[_random.Next(_pingReplies.Length - 1)]));
    }

    [Command("帮助", "^help$", Priority = 0)]
    public Task Help(MessageEventArgs eventArgs, string[] args)
    {
        var builder = new MessageBuilder("[Help]\n")
            .Text($"SuzuBot-{SuzuBotBuildStamp.Version}\n")
            .Text("AkulaKirov 2018 GPL-3.0\n")
            .Text(@"帮助文档请查阅: https://docs.suzubot.top/");
        return eventArgs.Reply(builder);
    }

    [Command("复读", "^echo (.*)", Priority = 0)]
    public Task Echo(MessageEventArgs eventArgs, string[] args)
    {
        return eventArgs.SendMessage(MessageBuilder.Eval(string.Join(' ', args)));
    }

    [Command("反馈", "^report (.*)", Priority = 0)]
    public Task Report(MessageEventArgs eventArgs, string[] args)
    {
        MessageBuilder builder = new MessageBuilder("[Report]\n")
            .Text($"来自用户 {eventArgs.Sender.Name}({eventArgs.Sender.Id})|{eventArgs.Subject.Name}({eventArgs.Subject.Id}) 的反馈\n")
            ;
        MessageBuilder evalBuilder = MessageBuilder.Eval(string.Join(' ', args));
        eventArgs.Bot.SendGroupMessage(RinBotGroup, builder + evalBuilder).Wait();
        builder = new MessageBuilder("[Report]\n反馈信息已发送");
        return eventArgs.Reply(builder);
    }

    [Command("用户信息", "^info$", Priority = 0)]
    public Task Info(MessageEventArgs eventArgs, string[] args)
    {
        var info = Context.DatabaseManager.GetUserInfo(eventArgs.Sender.Id);
        var builder = new MessageBuilder("[Info]\n")
            .Text($"{eventArgs.Sender.Name}\n")
            .Text($"SuzuCoin: {info.Coin} SC\n")
            .Text($"等级: {info.Level} Exp\n")
            .Text($"距离下一级所需经验值: {info.NextLevelExp} Exp\n")
            .Text($"权限组: {info.AuthGroup}\n");
        return eventArgs.Reply(builder);
    }

    [Command("退群", "^quit$", Priority = 0, SourceType = SourceType.Group, AuthGroup = AuthGroup.Admin, WarnOnAuthFail = true)]
    public Task Quit(GroupMessageEventArgs eventArgs, string[] args)
    {
        return eventArgs.Bot.GroupLeave(eventArgs.Subject.Id);
    }

    [Command("命令列表", "^cmdlist$", Priority = 0)]
    public Task CommandList(MessageEventArgs eventArgs, string[] args)
    {
        StringBuilder stringBuilder = new("[CMDLIST]\n");
        if (eventArgs is GroupMessageEventArgs)
        {
            var groupInfo = Context.DatabaseManager.GetGroupInfo(eventArgs.Subject.Id);
            foreach (var module in Context.ModuleManager.Modules)
            {
                char indicator = '○';
                if (groupInfo.Modules.Contains(module.GetType().Name))
                    indicator = '⌀';
                if (!module.IsEnabled)
                    indicator = '◆';
                stringBuilder.AppendLine($"{module.Name} {indicator}");
            }
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("○ - 开启");
            stringBuilder.AppendLine("⌀ - 群关闭");
            stringBuilder.AppendLine("◆ - 禁用");
        }
        else
        {
            foreach (var module in Context.ModuleManager.Modules)
            {
                stringBuilder.AppendLine(module.Name);
            }
        }

        return eventArgs.Reply(stringBuilder.ToString());
    }

    [Command("命令控制", "^cmdctl (enable|disable) (.+)", Priority = 0, SourceType = SourceType.Group, AuthGroup = AuthGroup.Admin, WarnOnAuthFail = true)]
    public Task CommandControl(GroupMessageEventArgs eventArgs, string[] args)
    {
        bool enable = args[0] == "enable";
        int counter = 0;
        SuzuGroupInfo groupInfo = Context.DatabaseManager.GetGroupInfo(eventArgs.Subject.Id);
        List<string> groupModules = groupInfo.Modules;
        string[] moduleNames = args[1].Split().Select(x => x.Trim()).ToArray();
        StringBuilder stringBuilder = new("[CMDCTL]\n");
        foreach (var name in moduleNames)
        {
            bool found = false;
            foreach (var module in Context.ModuleManager.Modules)
            {
                if (module.Name == name || module.GetType().Name == name)
                {
                    found = true;
                    var typeName = module.GetType().Name;
                    if (module.IsCritical)
                    {
                        stringBuilder.AppendLine($"不允许修改关键模块: {typeName}");
                    }
                    if (enable && groupModules.Contains(typeName))
                    {
                        groupModules.Remove(typeName);
                        stringBuilder.AppendLine($"已开启模块: {typeName}");
                        counter++;
                    }
                    else if (!enable && !groupModules.Contains(typeName))
                    {
                        groupModules.Add(typeName);
                        stringBuilder.AppendLine($"已关闭模块: {typeName}");
                        counter++;
                    }
                    break;
                }
            }

            if (!found)
                stringBuilder.AppendLine($"找不到模块: {name}");
        }

        groupInfo.Modules = groupModules;
        Context.DatabaseManager.UpdateGroupInfo(groupInfo);
        stringBuilder.AppendLine($"\n已完成了对 {counter} 个模块的更改");
        return eventArgs.Reply(stringBuilder.ToString());
    }
}
