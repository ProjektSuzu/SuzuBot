using Konata.Core;
using Konata.Core.Interfaces.Api;
using Newtonsoft.Json;
using RinBot.Core;
using RinBot.Core.Component.Command.CustomAttribute;
using RinBot.Core.Component.Event;
using RinBot.Core.Component.Message;
using RinBot.Core.Component.Message.Model;
using RinBot.Core.Component.Permission;
using System.Text;

namespace RinBot.Command
{
    [Module("签到", "org.akulak.sign")]
    internal class Sign
    {
        private static readonly string CHECH_IN_LIST_PATH = Path.Combine(Global.RESOURCE_PATH, "checkInList.json");
        private List<(string, EventSourceType)> checkInList;
        Timer clearTimer;

        public Sign()
        {
            if (!File.Exists(CHECH_IN_LIST_PATH))
                checkInList = new();
            else
                checkInList = JsonConvert.DeserializeObject<List<(string, EventSourceType)>>(File.ReadAllText(CHECH_IN_LIST_PATH)) ?? new();
            File.WriteAllTextAsync(CHECH_IN_LIST_PATH, JsonConvert.SerializeObject(checkInList));

            clearTimer = new Timer(new TimerCallback(ClearCheckInList));
            clearTimer.Change(DateTime.Now - new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day), new TimeSpan(24, 0, 0));
        }

        private void ClearCheckInList(object obj)
            => checkInList.Clear();

        private string GetMemoryStr(long memory)
        {
            if (Math.Abs(memory) < 1_000)
                return $"{memory} KB";
            else if (Math.Abs(memory) < 1_000_000)
                return $"{(float)memory / 1_000:0.000} MB";
            else if (Math.Abs(memory) < 1_000_000_000)
                return $"{(float)memory / 1_000_000:0.000} GB";
            else
                return $"{(float)memory / 1_000_000_000:0.000} TB";
        }

        [Command("签到", new[] { "sign", "签到", "打卡" }, (int)MatchingType.StartsWith, ReplyType.Reply)]
        public RinMessageChain OnSign(RinEvent e)
        {
            var chain = new RinMessageChain();
            var name = "";
            if (e.EventSourceType == EventSourceType.QQ)
            {
                var bot = e.OriginalSender as Bot;
                if (e.EventSubjectType == EventSubjectType.Group)
                {
                    name = bot.GetGroupMemberInfo(uint.Parse(e.SubjectId), uint.Parse(e.SenderId)).Result.NickName;
                }
                else
                {
                    name = bot.GetFriendList().Result.First(x => x.Uin.ToString() == e.SenderId).Name;
                }
            }
            else
            {
                return null;
            }
            chain.Add(TextChain.Create($"[Sign]\n{name}\n"));

            var seed = int.Parse(DateTime.Now.ToString("yyyyMMdd")) + uint.Parse(e.SenderId);
            while (seed > int.MaxValue)
                seed /= 2;
            var random = new Random((int)seed);
            StringBuilder signReply = new();

            if (checkInList.Any(x => x.Item1 == e.SenderId && x.Item2 == e.EventSourceType))
            {
                signReply.AppendLine("今天已经打过卡啦, 明天再来吧.\n");
            }
            else
            {
                checkInList.Add(new(e.SenderId, e.EventSourceType));
                int memory = new Random().Next(500, 1000);
                int exp = new Random().Next(5, 10);
                if (e.EventSourceType == EventSourceType.QQ)
                {
                    var info = PermissionManager.Instance.GetQQUserInfo(uint.Parse(e.SenderId));
                    info.Exp += exp;
                    info.Memory += memory;
                }
                else
                {
                    return null;
                }
                signReply.AppendLine($"打卡成功 你是今天第 {checkInList.Count} 个打卡的.");
                signReply.AppendLine($"经验+ {exp} exp.");
                signReply.AppendLine($"内存+ {GetMemoryStr(memory)}.\n");
            }
            chain.Add(TextChain.Create(signReply.ToString()));

            StringBuilder lotReply = new();
            (string lot, string comment) = Lot(random);
            lotReply.AppendLine($"今天的运势是: {lot}");
            lotReply.AppendLine($"{comment}\n");

            chain.Add(TextChain.Create(lotReply.ToString()));

            chain.Add(TextChain.Create("\n结果仅供参考, 自己的命运要自己把握哦(ﾉﾟ▽ﾟ)ﾉ"));
            return chain;
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
                        comment = LotComment.Random(LotComment.Just);
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
            public static string[] Just = {
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
