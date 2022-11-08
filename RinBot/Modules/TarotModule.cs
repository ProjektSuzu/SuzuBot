using System.Diagnostics.Metrics;
using System.Reflection;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using RinBot.Common;
using RinBot.Common.Attributes;
using RinBot.Common.EventArgs.Messages;

namespace RinBot.Modules;

[Module("塔罗牌")]
internal class TarotModule : BaseModule
{
    private static Dictionary<int, TarotCardInfo> _tarotCards = new();

    // 没有十年脑血栓写不出这些文本
    private string[] tarotDrawText = new[]
    {
            "{name} 的回合，Draw!",
            "正在为 {name} 洗牌中",
            "全能的塔罗牌啊，请为 {name} 指点迷津吧！",
    };

    public override void Init()
    {
        base.Init();
        var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, TarotCardInfo>>(File.ReadAllText(Path.Combine(ResourceDirPath, "descriptions.json")));
        var images = new DirectoryInfo(Path.Combine(ResourceDirPath, "Image")).EnumerateFiles().ToArray();
        foreach (var keyPair in jsonDict)
        {
            keyPair.Value.ImagePath = images.First(x => x.Name.StartsWith(keyPair.Key.PadLeft(2, '0'))).FullName;
            _tarotCards.Add(int.Parse(keyPair.Key), keyPair.Value);
        }
    }


    [Command("获取塔罗牌", "tarot", MatchType = Common.Attributes.MatchType.StartsWith)]
    public Task GetTarotCards(MessageEventArgs eventArgs, string[] args)
    {
        var messageBuilder = new MessageBuilder();
        int num = 1;
        if (args.Any())
        {
            if (!int.TryParse(args[0], out num) || num <= 0 || num > 6)
            {
                messageBuilder.Text($"[TarotCard]\n参数非法: {args[0]} => [<num>]");
                return eventArgs.Reply(messageBuilder);
            }
        }

        if (num == 1)
        {
            var tarot = RandomTarotCards()[0];
            messageBuilder.Text($"[TarotCard]\n{tarot.Name} {(tarot.IsReversed ? "逆位" : "正位")}\n");
            messageBuilder.Image(File.ReadAllBytes(tarot.ImagePath));
            messageBuilder.Text($"\n释义: \n{(tarot.IsReversed ? tarot.Info.ReverseDescribe : tarot.Info.Describe)}");
            return eventArgs.Reply(messageBuilder);
        }
        else
        {
            var multiMsg = new MultiMsgChain();
            multiMsg.Add(new MessageStruct(eventArgs.Bot.Uin, eventArgs.Bot.Name, new MessageBuilder("[TarotCard]\n" +
                tarotDrawText[new Random().Next(tarotDrawText.Length)].Replace(
                    "{name}",
                    eventArgs.SenderName)).Build()));

            var tarots = RandomTarotCards(num);
            int counter = 0;

            foreach (var tarot in tarots)
            {
                var cardMessageBuilder = new MessageBuilder();
                counter++;
                cardMessageBuilder.Text($"#{counter:00} : {tarot.Name} {(tarot.IsReversed ? "逆位" : "正位")}\n");
                cardMessageBuilder.Image(File.ReadAllBytes(tarot.ImagePath));
                cardMessageBuilder.Text($"\n释义: \n{(tarot.IsReversed ? tarot.Info.ReverseDescribe : tarot.Info.Describe)}");

                var messageStruct = new MessageStruct(eventArgs.Bot.Uin, eventArgs.Bot.Name, cardMessageBuilder.Build());
                multiMsg.Add(messageStruct);
            }

            messageBuilder.Add(multiMsg);
            return eventArgs.SendMessage(messageBuilder);
        }

    }

    public List<TarotCardInfo> RandomTarotCards(int num = 1, Random? random = null)
    {
        random ??= new();
        List<TarotCardInfo> tarots = new();
        foreach (var info in _tarotCards.Values)
        {
            tarots.Add(new TarotCardInfo()
            {
                Name = info.Name,
                NameEN = info.NameEN,
                ImagePath = info.ImagePath,
                Info = info.Info,
            });
        }
        tarots = tarots.OrderBy(x => random.Next()).Take(num).ToList();
        foreach (var info in tarots)
            info.IsReversed = random.Next(2) == 1;
        return tarots;
    }
}

public class TarotCardInfo
{
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("name_en")]
    public string NameEN { get; set; }
    [JsonIgnore]
    public string ImagePath { get; set; }
    [JsonIgnore]
    public bool IsReversed { get; set; }
    [JsonProperty("info")]
    public CardInfo Info { get; set; }

    public class CardInfo
    {
        [JsonProperty("element")]
        public string Element { get; set; }
        [JsonProperty("match")]
        public string Match { get; set; }
        [JsonProperty("celestial")]
        public string Celestial { get; set; }
        [JsonProperty("keyword")]
        public string Keyword { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("describe")]
        public string Describe { get; set; }
        [JsonProperty("reverse_describe")]
        public string ReverseDescribe { get; set; }
    }
}