﻿using System.Text.Json.Serialization;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;
using SuzuBot.Utils;

namespace SuzuBot.Modules.Entertaining;
public class TarotModule : BaseModule
{
    private Dictionary<int, TarotCardInfo> _tarotCards = new();

    public TarotModule()
    {
        Name = "塔罗牌";
    }

    // 没有十年脑血栓写不出这些文本
    private string[] tarotDrawText = new[]
    {
            "{name} 的回合，Draw!",
            "正在为 {name} 洗牌中",
            "全能的塔罗牌啊，请为 {name} 指点迷津吧！",
    };

    public override bool Init()
    {
        base.Init();
        var jsonDict = File.ReadAllText(Path.Combine(ResourceDirPath, "descriptions.json")).DeserializeJson<Dictionary<string, TarotCardInfo>>();
        var images = new DirectoryInfo(Path.Combine(ResourceDirPath, "Image")).EnumerateFiles().ToArray();
        foreach (var keyPair in jsonDict)
        {
            keyPair.Value.ImagePath = images.First(x => x.Name.StartsWith(keyPair.Key.PadLeft(2, '0'))).FullName;
            _tarotCards.Add(int.Parse(keyPair.Key), keyPair.Value);
        }
        return true;
    }

    [Command("获取塔罗牌", "^tarot\\s?([0-9]*)?")]
    public Task GetTarotCards(MessageEventArgs eventArgs, string[] args)
    {
        var messageBuilder = new MessageBuilder();
        int num = 1;
        if (args.Any())
        {
            if (!int.TryParse(args[0], out num) || num <= 0 || num >= 9)
            {
                messageBuilder.Text($"[Tarot]\n参数非法: {args[0]} => [<num>]");
                return eventArgs.Reply(messageBuilder);
            }
        }

        if (num == 1)
        {
            var tarot = RandomTarotCards()[0];
            messageBuilder.Text($"[Tarot]\n{tarot.Name} {(tarot.IsReversed ? "逆位" : "正位")}\n");
            messageBuilder.Image(File.ReadAllBytes(tarot.ImagePath));
            messageBuilder.Text($"\n释义: \n{(tarot.IsReversed ? tarot.Info.ReverseDescribe : tarot.Info.Describe)}");
            return eventArgs.Reply(messageBuilder);
        }
        else
        {
            var multiMsg = new MultiMsgChain
            {
                new MessageStruct(eventArgs.Bot.Uin, eventArgs.Bot.Name, new MessageBuilder("[Tarot]\n" +
                tarotDrawText[new Random().Next(tarotDrawText.Length)].Replace(
                    "{name}",
                    eventArgs.Sender.Name)).Build())
            };

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
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("name_en")]
    public string NameEN { get; set; }
    [JsonIgnore]
    public string ImagePath { get; set; }
    [JsonIgnore]
    public bool IsReversed { get; set; }
    [JsonPropertyName("info")]
    public CardInfo Info { get; set; }

    public class CardInfo
    {
        [JsonPropertyName("element")]
        public string Element { get; set; }
        [JsonPropertyName("match")]
        public string Match { get; set; }
        [JsonPropertyName("celestial")]
        public string Celestial { get; set; }
        [JsonPropertyName("keyword")]
        public string Keyword { get; set; }
        [JsonPropertyName("content")]
        public string Content { get; set; }
        [JsonPropertyName("describe")]
        public string Describe { get; set; }
        [JsonPropertyName("reverse_describe")]
        public string ReverseDescribe { get; set; }
    }
}