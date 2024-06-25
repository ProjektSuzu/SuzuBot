using System.CommandLine.Parsing;
using Lagrange.Core;
using Lagrange.Core.Common.Entity;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using SuzuBot.Commands;
using SuzuBot.Commands.Attributes;

namespace SuzuBot.Hosting;

internal class RequestContext(IServiceProvider services, BotContext bot, MessageChain chain)
{
    public IServiceProvider Services { get; } = services;
    public BotGroup Group { get; } =
        bot.FetchGroups().Result.FirstOrDefault(x => x.GroupUin == chain.GroupUin!)
        ?? bot.FetchGroups(true).Result.First(x => x.GroupUin == chain.GroupUin!);
    public BotGroupMember Member { get; } = chain.GroupMemberInfo!;
    public BotContext Bot { get; } = bot;
    public MessageChain Chain { get; } = chain;
    public string Input { get; set; } =
        string.Join(' ', chain.OfType<TextEntity>().Select(x => x.Text)).Trim();
    public Prefix MessagePrefix { get; set; } = Prefix.None;
    public Prefix CommandPrefix { get; set; } = Prefix.None;
    public SuzuCommand? Command { get; set; }
    public ParseResult? ParseResult { get; set; }
    public bool UseShortcut { get; set; } = false;
}
