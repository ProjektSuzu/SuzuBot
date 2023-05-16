using System.Text.RegularExpressions;
using Konata.Core.Message.Model;
using SuzuBot.Attributes;
using SuzuBot.EventArgs;
using SuzuBot.Extensions;

namespace SuzuBot.Modules.Core;

[SuzuModule("核心模块")]
internal class Core : BaseModule
{
    public Core(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    [SuzuCommand("Ping", "^ping$")]
    public Task Ping(MessageEventArgs eventArgs, GroupCollection collection)
    {
        eventArgs.Reply(TextChain.Create("Pong!"));
        return Task.CompletedTask;
    }

    [SuzuCommand("复读", "^reply$")]
    public async Task Reply(MessageEventArgs eventArgs, GroupCollection collection)
    {
        await eventArgs.Reply(TextChain.Create("Listening"));
        var args = await eventArgs.Next(TimeSpan.FromSeconds(60));
        if (args is null) await eventArgs.Reply(TextChain.Create("Cancelled"));
        else await eventArgs.Reply(args.Message.Chain);
    }
}
