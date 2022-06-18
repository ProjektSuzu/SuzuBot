using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using RinBot.Core.Attributes.Command.Modules;
using RinBot.Core.Attributes.CommandSet;
using RinBot.Utils.Database.Tables;
using SkiaSharp;

namespace RinBot.Commands.Modules
{
    [CommandSet("每日签到", "com.akulak.dailySign")]
    internal class DailySign : BaseCommand
    {
        [GroupMessageCommand("签到", new[] { @"^sign", @"^fortune", @"^打卡", @"^签到", @"^(今日)?运势" })]
        public void OnSign(Bot bot, GroupMessageEvent messageEvent)
        {
            string? reply = "";
            MessageBuilder? message = new MessageBuilder();
            uint uin = messageEvent.MemberUin;
            UserInfo? info = UserInfoManager.GetUserInfo(uin);
            //应该不会 但是以防万一
            if (info == null)
            {
                reply = $"错误: 找不到用户: \"U{uin}\".";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            if (DateTime.Today - info.lastSign < TimeSpan.FromDays(1))
            {
                reply = $"\n你今天已经打过卡啦, 明天再来吧.\n\n";
            }
            else
            {
                uint coin = (uint)new Random().Next(1000, 10001);
                int exp = new Random().Next(5, 51);

                info.exp += exp;
                info.coin += coin;
                info.lastSign = DateTime.Today;
                while (info.exp >= UserInfoManager.LevelToExp(info.level))
                {
                    info.exp -= UserInfoManager.LevelToExp(info.level);
                    info.level++;
                }

                if (!UserInfoManager.UpdateUserInfo(info))
                {
                    reply = $"错误: 更新用户信息时失败.";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }

                reply =
                $"\n打卡成功!\n" +
                $"经验+ {exp} exp\n" +
                $"内存+ {UserInfoManager.CoinToString(coin)}\n" +
                $"当前等级: {info.level}\n" +
                $"距离下一级还差: {UserInfoManager.LevelToExp(info.level) - info.exp} exp\n\n";
            }

            message.Text(reply);
            reply = "";

            long seed = int.Parse(DateTime.Today.ToString("yyyymmdd")) + uin;
            while (seed > int.MaxValue)
            {
                seed /= 2;
            }

            Random random = new Random((int)seed);


            (string lot, string comment) = Lot(random);

            reply =
                $"今天的运势是: {lot}\n" +
                $"{comment}\n" +
                $"\n";

            message.Text(reply);
            bool isReversed = random.Next(0, 2) == 0;

            TarotCard? card = Tarot.GetCards(1, random).First();
            reply =
                $"今天的塔罗牌是: {card.title} {(!isReversed ? "正位" : "逆位")}\n" +
                $"{(!isReversed ? card.positive : card.negative)}\n" +
                $"\n";

            SKBitmap tarotImg = SKBitmap.Decode(Tarot.GetCardCoverPath(card.title));
            //if (isReversed)
            //{
            //    SKBitmap flipImg = new SKBitmap(tarotImg.Width, tarotImg.Height);
            //    SKCanvas canvas = new SKCanvas(flipImg);
            //    canvas.Scale(-1, -1, tarotImg.Width / 2, tarotImg.Height / 2);
            //    canvas.DrawBitmap(tarotImg, 0, 0);
            //    tarotImg = flipImg.Copy();
            //    flipImg.Dispose();
            //    canvas.Dispose();
            //}

            message.Image(tarotImg.Encode(SKEncodedImageFormat.Jpeg, 80).ToArray()).Text(reply);
            reply = "\n结果仅供参考, 自己的命运要自己把握哦(ﾉﾟ▽ﾟ)ﾉ";


            message.Text(reply);
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder().At(uin).Add(message.Build()));
            tarotImg.Dispose();
            return;
        }

        public (string, string) Lot(Random random)
        {
            int rnd = random.Next(0, 5);
            string? result = "";
            string? comment = "";
            switch (rnd)
            {
                case 0:
                    {
                        result = "大凶";
                        comment = LotComment.Random(LotComment.Miss);
                        break;
                    }
                case 1:
                    {
                        result = "凶";
                        comment = LotComment.Random(LotComment.Bad);
                        break;
                    }
                case 2:
                    {
                        result = "小吉";
                        comment = LotComment.Random(LotComment.Good);
                        break;
                    }
                case 3:
                    {
                        result = "吉";
                        comment = LotComment.Random(LotComment.Great);
                        break;
                    }
                case 4:
                    {
                        result = "大吉";
                        comment = LotComment.Random(LotComment.Pure);
                        break;
                    }
            }

            return (result, comment);

        }

        private static class LotComment
        {
            //别问我为什么要这么命名 就是玩
            public static string[] Miss = {
            "铃会为你祈祷的...",
            "现在偷偷改签还来得及吗...",
            "也许还能再抢救一下...",
        };
            public static string[] Bad = {
            "今天还是小心一点的好..",
            "一定要好好的..",
            "这种事情也是没法控制的嘛.."
        };
            public static string[] Good = {
            "运气不错.",
            "美好的一天从此开始."
        };
            public static string[] Great = {
            "今天或许有意外的惊喜呢~",
            "I`m so happy~",
        };
            public static string[] Pure = {
            "这种运气..真的是存在的吗~~",
            "其实是铃偷偷帮你改的~不要告诉别人哦~~",
            "三☆倍☆Ice☆Cream~~"
        };

            public static string Random(IEnumerable<string> vs)
            {
                return vs.ElementAt(new Random().Next(vs.Count()));
            }
        }
    }
}
