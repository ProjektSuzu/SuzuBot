using System.Diagnostics;
using System.Text;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.DependencyInjection;
using SuzuBot.Commands.Attributes;
using SuzuBot.Hosting;
using SuzuBot.Services;

namespace SuzuBot.Modules;

[Module("CoreUtils")]
internal class CoreUtils
{
    private static readonly string[] _pingReplies =
    [
        "Pong!",
        "我还活着ヾ(•ω•`)o",
        "我得重新集结部队",
        "进攻D点",
        "Insufficient funds",
        "你现在不能休息，周围有怪物在游荡",
        "你必须先攻击那个具有嘲讽的随从",
        "欲强吃得苦中苦，C8延长2点5"
    ];

    [Command("Ping")]
    public Task Ping(RequestContext context)
    {
        return context.Bot.SendMessage(
            MessageBuilder
                .Group(context.Group.GroupUin)
                .Forward(context.Chain)
                .Text(_pingReplies[Random.Shared.Next(_pingReplies.Length)])
                .Build()
        );
    }

    [Command("运行状态", "stats")]
    public Task Status(RequestContext context)
    {
        var metrics = context.Services.GetRequiredService<BotMetrics>();
        var proc = Process.GetCurrentProcess();
        var memory = proc.PrivateMemorySize64 / 1024 / 1024;
        var uptime = DateTime.Now - proc.StartTime;

        var sb = new StringBuilder("[SuzuBot]\n============\n");
        sb.AppendLine($"运行时间: {uptime:c}");
        sb.AppendLine($"内存占用: {memory} MB");
        sb.AppendLine($"消息接收: {metrics.MessageCount}");
        sb.AppendLine($"命令调用: {metrics.CommandCount}");

        sb.Append("\nAkula Kirov GNU AGPL-3.0");
        return context.Bot.SendMessage(
            MessageBuilder.Group(context.Group.GroupUin).Text(sb.ToString()).Build()
        );
    }
}
