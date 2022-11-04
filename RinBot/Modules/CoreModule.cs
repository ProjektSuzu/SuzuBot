using Newtonsoft.Json;
using RinBot.Common;
using RinBot.Common.Attributes;
using RinBot.Common.EventArgs.Messages;

namespace RinBot.Modules;

[Module("核心模块", IsCritical = true)]
internal class CoreModule : BaseModule
{
    string[] _pingReplys;

    public override void Init()
    {
        base.Init();
        _pingReplys = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(Path.Combine(ResourceDirPath, "pingReplys.json")))!;
    }

    [Command("Ping", "ping", AuthFailWarning = true)]
    public Task PingReply(MessageEventArgs eventArgs, string[] args)
    {
        return eventArgs.Reply(_pingReplys[new Random().Next(_pingReplys.Length)]);
    }
}
