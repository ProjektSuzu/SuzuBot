using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.Utils.Database.Tables;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("每日签到", "com.akulak.dailySign")]
    internal class DailySignCommand : BaseCommand
    {
        public override string Help => $"[每日签到]\n" +
                $"/sign      进行当日签到并抽签\n" +
                $"           每天只能签到一次 每天的签都是一样的\n" +
                $"\n" +
                $"快捷名:\n" +
                $"/fortune\n" +
                $"/打卡\n" +
                $"/签到\n" +
                $"/运势\n" +
                $"";
        public override void OnInit()
        {

        }

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
                uint coin = (uint)new Random().Next(100, 4000);
                int exp = new Random().Next(5, 15);

                info.exp += exp;
                info.coin += coin;
                info.lastSign = DateTime.Today;
                if (info.exp >= UserInfoManager.LevelToExp(info.level))
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

            (string lot, string comment) = Lot(uin);

            reply =
                $"今天的运势是: {lot}\n" +
                $"{comment}\n" +
                $"\n";

            message.Text(reply);
            reply = "";

            long seed = int.Parse(DateTime.Today.ToString("yyyymmdd")) + uin;
            if (seed > int.MaxValue)
            {
                seed /= 2;
            }

            bool isReversed = new Random((int)seed).Next(0, 2) == 0;

            Tarot? card = TarotCommands.GetCards(1, (int)seed).First();
            reply =
                $"今天的塔罗牌是: {card.title} {(isReversed ? "正位" : "逆位")}\n" +
                $"{(isReversed ? card.positive : card.negative)}\n" +
                $"\n";

            message.Image(TarotCommands.GetCardCoverPath(card.title)).Text(reply);
            reply = "";

            reply = "\n结果仅供参考, 自己的命运要自己把握哦(ﾉﾟ▽ﾟ)ﾉ";

            message.Text(reply);
            reply = "";

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder().At(uin).Add(message.Build()));
            return;
        }

        public (string, string) Lot(uint uin)
        {
            long seed = int.Parse(DateTime.Today.ToString("yyyymmdd")) + uin;
            if (seed > int.MaxValue)
            {
                seed /= 2;
            }

            int random = new Random((int)seed).Next(0, 5);
            string? result = "";
            string? comment = "";
            switch (random)
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
