using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.Utils.Database;
using ProjektRin.Utils.Database.Tables;
using SQLite;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("每日签到")]
    internal class DailySignCommandSet : BaseCommand
    {
        private SQLiteConnection _db;
        public override void OnInit()
        {
            _db = DatabaseManager.Instance.dbConnection;
        }

        [GroupMessageCommand("签到", new[] { @"^sign", @"^fortune", @"^打卡", @"^签到", @"^(今日)?运势" })]
        public void OnSign(Bot bot, GroupMessageEvent messageEvent)
        {
            var reply = "";
            var uin = messageEvent.MemberUin;
            var info = UserInfoManager.GetUserInfo(uin);
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
                var exp = new Random().Next(5, 15);

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

            var (lot, comment) = Lot(uin);

            reply +=
                $"今天的运势是: {lot}\n" +
                $"{comment}\n" +
                $"";

            reply += "\n结果仅供参考, 自己的命运要自己把握哦(ﾉﾟ▽ﾟ)ﾉ";

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder().At(uin).Text(reply));
            return;
        }

        public (string, string) Lot(uint uin)
        {
            var seed = uint.Parse(DateTime.Today.ToString("yyyymmdd")) + uin;
            var random = new Random((int)seed).Next(0, 4);
            var result = "";
            var comment = "";
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

        static class LotComment
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
