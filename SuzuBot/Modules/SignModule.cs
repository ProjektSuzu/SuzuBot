using Konata.Core.Message;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;
using SuzuBot.Utils;

namespace SuzuBot.Modules;
public class SignModule : BaseModule
{
    private string _signListPath;
    private SignList _signList;
    private Timer _clearTimer;
    public SignModule()
	{
		Name = "签到";
	}
    public override bool Init()
    {
        base.Init();
        _signListPath = Path.Combine(ResourceDirPath, "signList.json");
        if (!File.Exists(_signListPath))
        {
            _signList = new SignList();
        }
        else
        {
            _signList = File.ReadAllText(_signListPath).DeserializeJson<SignList>()
                           ?? new();
            _signList.Flush();
            SaveList();
        }

        _clearTimer = new Timer(new TimerCallback((_) => _signList.Flush(true)));
        _clearTimer.Change(DateTime.Today.AddDays(1) - DateTime.Now, new TimeSpan(24, 0, 0));
        return true;
    }

    [Command("签到", "^sign$", "^签到$", "^打卡$")]
    public async Task Sign(MessageEventArgs messageEvent, string[] args)
    {
        var builder = new MessageBuilder("[Sign]\n");
        // 万恶的 RandomNumberGenerator 额鹅鹅鹅啊啊啊啊
        var seed = int.Parse(DateTime.Today.ToString("yyyyMMdd")) + messageEvent.Sender.Id;
        var fortuneRandom = new Random((int)seed);

        if (_signList.List.TryGetValue(messageEvent.Sender.Id, out var sign) && DateTime.Today == sign.LastSign.Date)
        {
            builder.Text("你今天已经签到过了");
            await messageEvent.Reply(builder);
            return;
        }
        else
        {
            sign = _signList.Sign(messageEvent.Sender.Id);
            SaveList();

            // 基础部分
            var random = new Random();
            var coin = random.Next(1, 100);
            var exp = random.Next(1, 10);

            if (sign.ContinuousSign % 7 == 0 && sign.ContinuousSign > 0)
                coin += 100;
            if (_signList.SignCountToday == 1)
            {
                coin += 100;
                exp += 10;
            }
            else if (_signList.SignCountToday == 114)
            {
                coin = 514;
                exp = 114;
            }

            var info = Context.DatabaseManager.GetUserInfo(messageEvent.Sender.Id);
            info.Coin += coin;
            bool upgrade = info.GiveExp((uint)exp);
            _ = Context.DatabaseManager.UpdateUserInfo(info);
            builder.Text($"{messageEvent.Sender.Name}\n签到成功\n" +
                $"你是今天第 {_signList.SignCountToday} 个签到的\n" +
                $"{(sign.ContinuousSign > 1 ? $"你已连续签到 {sign.ContinuousSign} 天\n" : "")}" +
                $"SC +{coin}\n" +
                $"经验 +{exp}\n" +
                (upgrade ? $"升级了ヾ(≧▽≦*)o\n当前等级: {info.Level}\n" : $"距离 {info.Level + 1} 级还需 {info.NextLevelExp} exp\n"));
        }

        // 抽签部分
        var stick = FortuneStick.GetStick(fortuneRandom);
        builder.Text($"\n今日的运势是: {stick.GetFortuneName()}\n" +
            $"{stick.GetComment()}\n\n");

        // 免责声明
        builder.Text($"结果仅供参考, 自己的命运要自己把握哦(・ω≦)☆");
        await messageEvent.Reply(builder);
    }

    private void SaveList()
        => File.WriteAllBytesAsync(_signListPath, _signList.SerializeJsonByteArray());
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
            _ => "???"
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
            _ => "???"
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
