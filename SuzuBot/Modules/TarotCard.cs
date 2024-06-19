using System.Text;
using System.Text.Json;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using SkiaSharp;
using SuzuBot.Commands.Attributes;
using SuzuBot.Hosting;

namespace SuzuBot.Modules;

// Resource from: MinatoAquaCrews/nonebot_plugin_tarot
// MIT License

[Module("塔罗牌")]
internal class TarotCard
{
    private readonly TarotCardInfo[] _tarots = JsonSerializer.Deserialize<TarotCardInfo[]>(
        File.ReadAllBytes(Path.Combine("resources", nameof(TarotCard), "tarot.json"))
    )!;

    [Command("抽塔罗牌", "tarot", "塔罗牌")]
    public Task SlotTarot(RequestContext context, int num = 1)
    {
        if (num == 1)
        {
            var card = GetTarots()[0];
            var image = GetImage(card.Tarot);

            var sb = new StringBuilder();
            sb.AppendLine($"{card.Tarot.name_cn} {(card.isReversed ? "逆位" : "正位")}");
            sb.AppendLine(
                $"释义: {(card.isReversed ? card.Tarot.meaning.down : card.Tarot.meaning.up)}"
            );
            if (card.isReversed)
            {
                using var skBitmap = SKBitmap.Decode(image);
                using var surface = new SKCanvas(skBitmap);
                surface.RotateDegrees(180, skBitmap.Width / 2, skBitmap.Height / 2);
                surface.DrawBitmap(skBitmap, 0, 0);
                using var data = skBitmap.Encode(SKEncodedImageFormat.Png, 100);
                image = data.ToArray();
            }
            return context.Bot.SendMessage(
                MessageBuilder
                    .Group(context.Group.GroupUin)
                    .Text(sb.ToString())
                    .Image(image)
                    .Build()
            );
        }
        else
        {
            var cards = GetTarots(num);
            List<MessageBuilder> builders = [];
            foreach (var (card, isReversed) in cards)
            {
                var builder = MessageBuilder.FakeGroup(context.Group.GroupUin, context.Bot.BotUin);
                var image = GetImage(card);
                var sb = new StringBuilder();
                sb.AppendLine($"{card.name_cn} {(isReversed ? "逆位" : "正位")}");
                sb.AppendLine($"释义: {(isReversed ? card.meaning.down : card.meaning.up)}");
                if (isReversed)
                {
                    using var skBitmap = SKBitmap.Decode(image);
                    using var surface = new SKCanvas(skBitmap);
                    surface.RotateDegrees(180, skBitmap.Width / 2, skBitmap.Height / 2);
                    surface.DrawBitmap(skBitmap, 0, 0);
                    using var data = skBitmap.Encode(SKEncodedImageFormat.Png, 100);
                    image = data.ToArray();
                }
                builder.Text(sb.ToString()).Image(image);
                builders.Add(builder);
            }

            return context.Bot.SendMessage(
                MessageBuilder
                    .Group(context.Group.GroupUin)
                    .MultiMsg(context.Group.GroupUin, [.. builders])
                    .Build()
            );
        }
    }

    private byte[] GetImage(TarotCardInfo tarot)
    {
        var path = Path.Combine("resources", nameof(TarotCard), tarot.type, $"{tarot.pic}.jpg");
        return File.ReadAllBytes(path);
    }

    private (TarotCardInfo Tarot, bool isReversed)[] GetTarots(
        int num = 1,
        Random? random = default
    )
    {
        random ??= Random.Shared;
        return _tarots
            .OrderBy(_ => random.Next())
            .Take(num)
            .Select(x => (x, random.Next() % 2 == 1))
            .ToArray();
    }
}

public class TarotCardInfo
{
    public string name_cn { get; set; }
    public string name_en { get; set; }
    public string type { get; set; }
    public Meaning meaning { get; set; }
    public string pic { get; set; }
}

public class Meaning
{
    public string up { get; set; }
    public string down { get; set; }
}
