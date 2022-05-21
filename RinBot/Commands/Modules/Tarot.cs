using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using RinBot.Core.Attributes.Command.Modules;
using RinBot.Core.Attributes.CommandSet;
using RinBot.Core.Components;
using SkiaSharp;

namespace RinBot.Commands.Modules
{
    [CommandSet("塔罗牌", "com.akulak.tarot")]
    internal class Tarot : BaseCommand
    {
        private static DirectoryInfo coverDir;
        public static List<TarotCard> tarots;

        public override void OnInit()
        {
            string? jsonPath = Path.Combine(BotManager.resourcePath, "tarot.json");
            tarots = JsonConvert.DeserializeObject<List<TarotCard>>(File.ReadAllText(jsonPath))!;
            coverDir = new DirectoryInfo(Path.Combine(BotManager.resourcePath, "Tarot"));
        }

        [GroupMessageCommand("塔罗牌", new[] { @"^tarot\s?([\s\S]+)?", @"^塔罗牌\s?([\s\S]+)?" })]
        public void OnTarot(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string[]? magicWords = new[]
            {
                "全知全能的水晶球啊, 请为迷茫之人点明方向吧!",
            };

            MultiMsgChain? multiReply = MultiMsgChain.Create();
            string? thing = "";
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

            while (seed > int.MaxValue)
            {
                seed /= 2;
            }

            List<TarotCard>? pickedCards = GetCards(3, new Random((int)seed));

            //为了避免在循环里上传图片的时候每次都要等待 异步方法顺序又会乱 只能用这种铸币办法了

            TarotCard? card1 = pickedCards[0];
            TarotCard? card2 = pickedCards[1];
            TarotCard? card3 = pickedCards[2];

            string? reply1 = "";
            string? title1 = card1.title;
            bool IsReversed1 = new Random().Next(2) == 0;
            string? description1 = !IsReversed1 ? card1.positive : card1.negative;
            string? coverPath1 = coverDir.GetFiles().First(x => x.Name.StartsWith(title1)).FullName;
            reply1 = $"\n开端 {title1}: {(!IsReversed1 ? "正位" : "逆位")}\n{description1}";

            string? reply2 = "";
            string? title2 = card2.title;
            bool IsReversed2 = new Random().Next(2) == 0;
            string? description2 = !IsReversed2 ? card2.positive : card2.negative;
            string? coverPath2 = coverDir.GetFiles().First(x => x.Name.StartsWith(title2)).FullName;
            reply2 = $"\n过程 {title2}: {(!IsReversed2 ? "正位" : "逆位")}\n{description2}";

            string? reply3 = "";
            string? title3 = card3.title;
            bool IsReversed3 = new Random().Next(2) == 0;
            string? description3 = !IsReversed3 ? card3.positive : card3.negative;
            string? coverPath3 = coverDir.GetFiles().First(x => x.Name.StartsWith(title3)).FullName;
            reply3 = $"\n结局 {title3}: {(!IsReversed3 ? "正位" : "逆位")}\n{description3}";

            SKBitmap tarotImg1 = SKBitmap.Decode(coverPath1);
            //if (IsReversed1)
            //{
            //    SKBitmap flipImg = new SKBitmap(tarotImg1.Width, tarotImg1.Height);
            //    SKCanvas canvas = new SKCanvas(flipImg);
            //    canvas.Scale(-1, -1, tarotImg1.Width / 2, tarotImg1.Height / 2);
            //    canvas.DrawBitmap(tarotImg1, 0, 0);
            //    tarotImg1 = flipImg.Copy();
            //    flipImg.Dispose();
            //    canvas.Dispose();
            //}

            SKBitmap tarotImg2 = SKBitmap.Decode(coverPath2);
            //if (IsReversed2)
            //{
            //    SKBitmap flipImg = new SKBitmap(tarotImg2.Width, tarotImg2.Height);
            //    SKCanvas canvas = new SKCanvas(flipImg);
            //    canvas.Scale(-1, -1, tarotImg2.Width / 2, tarotImg2.Height / 2);
            //    canvas.DrawBitmap(tarotImg2, 0, 0);
            //    tarotImg2 = flipImg.Copy();
            //    flipImg.Dispose();
            //    canvas.Dispose();
            //}

            SKBitmap tarotImg3 = SKBitmap.Decode(coverPath3);
            //if (IsReversed3)
            //{
            //    SKBitmap flipImg = new SKBitmap(tarotImg3.Width, tarotImg3.Height);
            //    SKCanvas canvas = new SKCanvas(flipImg);
            //    canvas.Scale(-1, -1, tarotImg3.Width / 2, tarotImg3.Height / 2);
            //    canvas.DrawBitmap(tarotImg3, 0, 0);
            //    tarotImg3 = flipImg.Copy();
            //    flipImg.Dispose();
            //    canvas.Dispose();
            //}

            multiReply
                    .AddMessage(new MessageStruct(bot.Uin, bot.Name, new MessageBuilder(magicWords.ElementAt(new Random().Next(magicWords.Length))).Build()))
                    .AddMessage(new MessageStruct(bot.Uin, bot.Name, new MessageBuilder($"水晶球回应了 {messageEvent.MemberCard} {(thing == "" ? "" : $"的\"{thing}\"")}\n" +
                    $"3张卡牌浮现了出来...").Build()))
                    .AddMessage(new MessageStruct(bot.Uin, bot.Name, new MessageBuilder()
                    .Image(tarotImg1.Encode(SKEncodedImageFormat.Jpeg, 80).ToArray())
                    .Text(reply1).Build()))
                    .AddMessage(new MessageStruct(bot.Uin, bot.Name, new MessageBuilder()
                    .Image(tarotImg2.Encode(SKEncodedImageFormat.Jpeg, 80).ToArray())
                    .Text(reply2).Build()))
                    .AddMessage(new MessageStruct(bot.Uin, bot.Name, new MessageBuilder()
                    .Image(tarotImg3.Encode(SKEncodedImageFormat.Jpeg, 80).ToArray())
                    .Text(reply3).Build()));


            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(multiReply));

            tarotImg1.Dispose();
            tarotImg2.Dispose();
            tarotImg3.Dispose();


            return;
        }

        public static List<TarotCard> GetCards(int num, Random random)
        {
            return tarots.OrderBy(x => random.Next()).Take(num).ToList();
        }

        public static string GetCardCoverPath(string title)
        {
            return coverDir.GetFiles().First(x => x.Name.StartsWith(title)).FullName;
        }
    }

    internal class TarotCard
    {
        public string title;
        public string positive;
        public string negative;
        public TarotCard(string name, string positive, string negative)
        {
            title = name;
            this.positive = positive;
            this.negative = negative;
        }
    }

}
