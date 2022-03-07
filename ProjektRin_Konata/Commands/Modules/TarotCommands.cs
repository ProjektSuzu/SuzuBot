using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.System;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("塔罗牌")]
    internal class TarotCommands : BaseCommand
    {
        public override string Help =>
            $"[塔罗牌]\n" +
                $"/tarot [<question>]      让塔罗牌回应你的请求\n" +
                $"\n" +
                $"  question    所想解答的问题\n" +
                $"\n" +
                $"快捷名:\n" +
                $"/塔罗牌\n" +
                $"\n\n(图一乐 别真信)";

        private static DirectoryInfo coverDir;
        public static List<Tarot> tarots;

        public override void OnInit()
        {
            var jsonPath = Path.Combine(BotManager.resourcePath, "tarot.json");
            tarots = JsonConvert.DeserializeObject<List<Tarot>>(File.ReadAllText(jsonPath))!;
            coverDir = new DirectoryInfo(Path.Combine(BotManager.resourcePath, "Tarot"));
        }

        [GroupMessageCommand("塔罗牌", new[] { @"^tarot\s?([\s\S]+)?", @"^塔罗牌\s?([\s\S]+)?" })]
        public void OnTarot(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var magicWords = new[]
            {
                "全知全能的水晶球啊, 请为迷茫之人点明方向吧!",
            };

            var multiReply = MultiMsgChain.Create();
            var sourceInfo = new SourceInfo(bot.Uin, bot.Name);
            var thing = "";
            if (args.Count > 0)
            {
                thing = args[0];
            }

            long seed = (int)(DateTime.Now.Year +
                DateTime.Now.Month +
                DateTime.Now.Day +
                DateTime.Now.Hour +
                DateTime.Now.Minute +
                DateTime.Now.Second + 
                messageEvent.MemberUin);

            if (seed > int.MaxValue)
                seed /= 2;

            var pickedCards = GetCards(3, (int)seed);

            //为了避免在循环里上传图片的时候每次都要等待 异步方法顺序又会乱 只能用这种铸币办法了

            var card1 = pickedCards[0];
            var card2 = pickedCards[1];
            var card3 = pickedCards[2];

            var reply1 = "";
            var title1 = card1.title;
            var IsReversed1 = new Random().Next(2) == 0;
            var description1 = IsReversed1 ? card1.positive : card1.negative;
            var coverPath1 = coverDir.GetFiles().First(x => x.Name.StartsWith(title1)).FullName;
            reply1 = $"\n开端 {title1}: {(IsReversed1 ? "正位" : "逆位")}\n{description1}";

            var reply2 = "";
            var title2 = card2.title;
            var IsReversed2 = new Random().Next(2) == 0;
            var description2 = IsReversed2 ? card2.positive : card2.negative;
            var coverPath2 = coverDir.GetFiles().First(x => x.Name.StartsWith(title2)).FullName;
            reply2 = $"\n过程 {title2}: {(IsReversed2 ? "正位" : "逆位")}\n{description2}";

            var reply3 = "";
            var title3 = card3.title;
            var IsReversed3 = new Random().Next(2) == 0;
            var description3 = IsReversed3 ? card3.positive : card3.negative;
            var coverPath3 = coverDir.GetFiles().First(x => x.Name.StartsWith(title3)).FullName;
            reply3 = $"\n结局 {title3}: {(IsReversed3 ? "正位" : "逆位")}\n{description3}";

            multiReply
                .AddMessage(sourceInfo, new MessageBuilder(magicWords.ElementAt(new Random().Next(magicWords.Length))))
                .AddMessage(sourceInfo, new MessageBuilder($"水晶球回应了 {messageEvent.MemberCard} {(thing == "" ? "" : $"的\"{thing}\"")}\n" +
                $"3张卡牌浮现了出来..."))
                .AddMessage(sourceInfo, new MessageBuilder()
                .Image(coverPath1)
                .Text(reply1))
                .AddMessage(sourceInfo, new MessageBuilder()
                .Image(coverPath2)
                .Text(reply2))
                .AddMessage(sourceInfo, new MessageBuilder()
                .Image(coverPath3)
                .Text(reply3));


            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(multiReply));
            return;
        }

        public static List<Tarot> GetCards(int num, int seed)
        {
            var rnd = new Random(seed);
            return tarots.OrderBy(x => rnd.Next()).Take(num).ToList();
        }

        public static string GetCardCoverPath(string title)
        {
            return coverDir.GetFiles().First(x => x.Name.StartsWith(title)).FullName;
        }
    }

    class Tarot
    {
        public string title;
        public string positive;
        public string negative;
        public Tarot(string name, string positive, string negative)
        {
            this.title = name;
            this.positive = positive;
            this.negative = negative;
        }
    }

}
