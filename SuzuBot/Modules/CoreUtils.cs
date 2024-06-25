using System.Diagnostics;
using System.Text;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.DependencyInjection;
using SuzuBot.Commands.Attributes;
using SuzuBot.Database;
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

    [Command("开关命令", "toggle")]
    [RouteRule(Permission = Permission.Admin)]
    [Shortcut("^(开启|启用)命令(.*)", "allow $2", Prefix = Prefix.None)]
    [Shortcut("^(关闭|禁用)命令(.*)", "deny $2", Prefix = Prefix.None)]
    public Task ToggleCommand(RequestContext context, string rule, string cmdName)
    {
        if (rule is not "allow" and not "deny")
        {
            return context.Bot.SendMessage(
                MessageBuilder.Group(context.Group.GroupUin).Text($"规则只能为 allow 或 deny").Build()
            );
        }
        var cmdManager = context.Services.GetRequiredService<CommandManager>();
        var cmd = cmdManager.Commands.FirstOrDefault(c =>
            c.Id == cmdName || c.Name == cmdName || c.InnerCommand.Aliases.Contains(cmdName)
        );
        if (cmd is null)
        {
            return context.Bot.SendMessage(
                MessageBuilder
                    .Group(context.Group.GroupUin)
                    .Forward(context.Chain)
                    .Text($"未找到命令: {cmdName}")
                    .Build()
            );
        }
        else if (cmd.ModuleType == typeof(CoreUtils))
        {
            return context.Bot.SendMessage(
                MessageBuilder
                    .Group(context.Group.GroupUin)
                    .Forward(context.Chain)
                    .Text($"无法调整核心命令: {cmdName}")
                    .Build()
            );
        }

        var dbCtx = context.Services.GetRequiredService<SuzuDbContext>();
        var groupRule = dbCtx.GroupRules.FirstOrDefault(x =>
            x.GroupUin == context.Group.GroupUin && x.CommandId == cmd.Id
        );
        if (groupRule is null)
        {
            dbCtx.GroupRules.Add(
                new()
                {
                    GroupUin = context.Group.GroupUin,
                    CommandId = cmd.Id,
                    Rule = rule
                }
            );
        }
        else
        {
            groupRule.Rule = rule;
            dbCtx.Update(groupRule);
        }

        dbCtx.SaveChanges();
        return context.Bot.SendMessage(
            MessageBuilder
                .Group(context.Group.GroupUin)
                .Forward(context.Chain)
                .Text($"命令 {cmdName} 已被设置为 {rule}")
                .Build()
        );
    }

    [Command("查看规则集", "rules")]
    public Task ShowRules(RequestContext context, uint groupUin = 0)
    {
        if (groupUin == 0)
            groupUin = context.Group.GroupUin;
        var dbCtx = context.Services.GetRequiredService<SuzuDbContext>();
        var rules = dbCtx.GroupRules.Where(x => x.GroupUin == groupUin).ToList();
        if (rules.Count == 0)
        {
            return context.Bot.SendMessage(
                MessageBuilder
                    .Group(context.Group.GroupUin)
                    .Forward(context.Chain)
                    .Text("当前群组没有设置规则")
                    .Build()
            );
        }
        else
        {
            var sb = new StringBuilder();
            foreach (var rule in rules)
                sb.AppendLine($"{rule.CommandId}={rule.Rule}");

            return context.Bot.SendMessage(
                MessageBuilder
                    .Group(context.Group.GroupUin)
                    .Forward(context.Chain)
                    .Text(sb.ToString())
                    .Build()
            );
        }
    }
}
