using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using SuzuBot.Commands.Attributes;
using SuzuBot.Hosting;

namespace SuzuBot.Modules;

[Module("表情包")]
internal class Sticker
{
    [Command("天宫心", "amm", "阿喵喵", "啊喵喵", "天宫心")]
    public Task Amamya(RequestContext context)
    {
        var imgs = Directory.GetFiles(
            Path.Combine(AppContext.BaseDirectory, "resources", nameof(Sticker), nameof(Amamya))
        );
        return context.Bot.SendMessage(
            MessageBuilder
                .Group(context.Group.GroupUin)
                .Image(File.ReadAllBytes(imgs[Random.Shared.Next(imgs.Length)]))
                .Build()
        );
    }

    [Command("丁真", "丁真", "一眼丁真")]
    public Task Dingzhen(RequestContext context)
    {
        var imgs = Directory.GetFiles(
            Path.Combine(AppContext.BaseDirectory, "resources", nameof(Sticker), nameof(Dingzhen))
        );
        return context.Bot.SendMessage(
            MessageBuilder
                .Group(context.Group.GroupUin)
                .Image(File.ReadAllBytes(imgs[Random.Shared.Next(imgs.Length)]))
                .Build()
        );
    }

    [Command("龙图", "龙图", "longtu")]
    public Task Dragon(RequestContext context)
    {
        var imgs = Directory.GetFiles(
            Path.Combine(AppContext.BaseDirectory, "resources", nameof(Sticker), nameof(Dragon))
        );
        return context.Bot.SendMessage(
            MessageBuilder
                .Group(context.Group.GroupUin)
                .Image(File.ReadAllBytes(imgs[Random.Shared.Next(imgs.Length)]))
                .Build()
        );
    }
}
