using Konata.Core.Message;
using Newtonsoft.Json;
using RinBot.Command.TarotCard;
using RinBot.Core;
using RinBot.Core.Components.Attributes;
using RinBot.Core.KonataCore.Events;
using System.Security.Cryptography;

namespace RinBot.Command.Sign
{
    [Module("签到", "AkulaKirov.Sign")]
    internal class SignModule
    {
        private static readonly string RESOURCE_DIR_PATH = Path.Combine(GlobalScope.RESOURCE_DIR_PATH, "AkulaKirov.Sign");
        private static readonly string SIGN_LIST_PATH = Path.Combine(RESOURCE_DIR_PATH, "signList.json");
        public SignModule()
        {
            Directory.CreateDirectory(RESOURCE_DIR_PATH);
            if (!File.Exists(SIGN_LIST_PATH))
            {
                signList = new SignList();
            }
            else
            {
                signList = JsonConvert.DeserializeObject<SignList>(File.ReadAllText(SIGN_LIST_PATH))
                           ?? new();
                signList.Flush();
                File.WriteAllTextAsync(SIGN_LIST_PATH, JsonConvert.SerializeObject(signList));
            }

            clearTimer = new Timer(new TimerCallback((obj) => signList.Flush(true)));
            clearTimer.Change(DateTime.Today.AddDays(1) - DateTime.Now, new TimeSpan(24, 0, 0));
        }
        private SignList signList;
        private Timer clearTimer;
        private void SaveList() => File.WriteAllTextAsync(SIGN_LIST_PATH, JsonConvert.SerializeObject(signList));



        [TextCommand("签到", new[] { "sign", "签到", "打卡" })]
        public void OnSign(MessageEventArgs messageEvent)
        {
            var builder = new MessageBuilder("[Sign]\n");
            // 万恶的 RandomNumberGenerator 额鹅鹅鹅啊啊啊啊
            var seed = int.Parse(DateTime.Today.ToString("yyyyMMdd")) + messageEvent.Sender.Uin;
            var fortuneRandom = new Random((int)seed);

            if (signList.List.TryGetValue(messageEvent.Sender.Uin, out var sign) && DateTime.Today == sign.LastSign.Date)
            {
                builder.Text($"{messageEvent.Sender.Name}\n你今天已经签到过了");
                messageEvent.Reply(builder);
                return;
            }
            else
            {
                sign = signList.Sign(messageEvent.Sender.Uin);
                SaveList();

                // 基础部分
                var random = new Random();
                var coin = random.Next(1, 100);
                var favor = random.Next(1, 10);

                var info = GlobalScope.PermissionManager.GetUserInfo(messageEvent.Sender.Uin);
                info.Coin += coin;
                info.Favor += favor;
                GlobalScope.PermissionManager.UpdateUserInfo(info);
                builder.Text($"{messageEvent.Sender.Name}\n签到成功\n" +
                    $"你是今天第 {signList.SignCountToday} 个签到的\n" +
                    $"{(sign.ContinuousSign > 1 ? $"你已连续签到 {sign.ContinuousSign} 天\n" : "")}" +
                    $"RC +{coin}\n" +
                    $"好感度 +{favor}\n");
            }

            // 抽签部分
            var stick = FortuneStick.GetStick(fortuneRandom);
            builder.Text($"今日的运势是: {stick.GetFortuneName()}\n" +
                $"{stick.GetComment()}\n\n");

            //// 塔罗牌部分
            //var tarot = TarotCards.GetTarotCards(1, fortuneRandom).First();
            //builder.Text($"今日的塔罗牌是: {tarot.Name} {(tarot.IsReversed ? "逆位" : "正位")}\n");
            //builder.Image(File.ReadAllBytes(tarot.ImagePath));
            //builder.Text($"\n释义: \n{(tarot.IsReversed ? tarot.Info.ReverseDescribe : tarot.Info.Describe)}\n\n");
            builder.Text($"为避免刷屏 自 RinBot-4.1.2 后不再在签到模块中提供塔罗牌抽卡\n" +
                $"请自行使用 /tarot 进行抽取\n\n");

            // 免责声明
            builder.Text($"结果仅供参考, 自己的命运要自己把握哦(・ω≦)☆");
            messageEvent.Reply(builder);
        }
    }

    internal class SignList
    {
        public Dictionary<uint, SignInfo> List { get; set; } = new();
        public DateTime DateTime { get; set; } = DateTime.Now;
        public uint SignCountToday { get; set; } = 0u;
        public void Flush(bool clearCounter = false)
        {
            List<SignInfo> temp = List.Values.ToList();
            temp.RemoveAll(x => DateTime.Today - x.LastSign > new TimeSpan(24, 0, 0));
            if (clearCounter || DateTime.Today - DateTime > new TimeSpan(24, 0, 0))
                SignCountToday = 0;
            List.Clear();
            foreach (var sign in temp)
            {
                List.Add(sign.Uin, sign);
            }
        }
        public SignInfo Sign(uint Uin)
        {
            SignCountToday++;
            DateTime = DateTime.Now;
            if (List.TryGetValue(Uin, out var sign))
            {
                sign.LastSign = DateTime;
                sign.ContinuousSign++;
            }
            else
            {
                List.Add(Uin, new SignInfo() { Uin = Uin });
            }
            return List[Uin];
        }
    }

    internal class SignInfo
    {
        public uint Uin { get; set; } = 0u;
        public uint ContinuousSign { get; set; } = 1u;
        public DateTime LastSign { get; set; } = DateTime.Today;
    }

    internal class FortuneStick
    {
        public enum FortuneStickType
        {
            RIP,                    // 大凶
            Suck,                   // 凶
            Nice,                   // 小吉
            Wunderbar,              // 吉
            OMGYouAreTheChoosenOne  // 大吉
        }

        public FortuneStickType FortuneType { get; private set; }
        private FortuneStick(FortuneStickType type)
        {
            FortuneType = type;
        }

        public static FortuneStick GetStick(Random? random = null)
        {
            random ??= new();
            return new FortuneStick((FortuneStickType)random.Next(5));
        }

        public string GetComment()
        {
            var random = new Random();
            return FortuneType switch
            {
                FortuneStickType.RIP
                => LotComment.Miss[random.Next(LotComment.Miss.Length)],
                FortuneStickType.Suck
                => LotComment.Bad[random.Next(LotComment.Bad.Length)],
                FortuneStickType.Nice
                => LotComment.Just[random.Next(LotComment.Just.Length)],
                FortuneStickType.Wunderbar
                => LotComment.Great[random.Next(LotComment.Great.Length)],
                FortuneStickType.OMGYouAreTheChoosenOne
                => LotComment.Pure[random.Next(LotComment.Pure.Length)],
            };
        }
        public string GetFortuneName()
        {
            return FortuneType switch
            {
                FortuneStickType.RIP => "大凶",
                FortuneStickType.Suck => "凶",
                FortuneStickType.Nice => "小吉",
                FortuneStickType.Wunderbar => "吉",
                FortuneStickType.OMGYouAreTheChoosenOne => "大吉",
            };
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
            "美好的一天从此开始.",
            "不错的开端.",
            };
            public static string[] Great = {
            "今天或许有意外的惊喜呢~",
            "I`m so happy~",
            "是吉不是寄哦~",
            };
            public static string[] Pure = {
            "你就是天选之人~~",
            "这种运气..真的是存在的吗~~",
            "其实是铃偷偷帮你改的~不要告诉别人哦~~",
            "三☆倍☆Ice☆Cream~~"
            };
        }
    }
}
