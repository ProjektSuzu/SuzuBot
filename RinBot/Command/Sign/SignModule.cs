using Konata.Core.Message;
using Newtonsoft.Json;
using RinBot.Core;
using RinBot.Core.Components.Attributes;
using RinBot.Core.KonataCore.Events;

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

            clearTimer = new Timer(new TimerCallback((obj) => signList.Flush()));
            clearTimer.Change(DateTime.Today.AddDays(1) - DateTime.Now, new TimeSpan(24, 0, 0));
        }
        private SignList signList;
        private Timer clearTimer;
        private void SaveList() => File.WriteAllTextAsync(SIGN_LIST_PATH, JsonConvert.SerializeObject(signList));

        [Command("签到", new[] { "sign", "签到" , "打卡" })]
        public void OnSign(MessageEventArgs messageEvent)
        {
            var builder = new MessageBuilder("[Sign]\n");
            if (signList.List.TryGetValue(messageEvent.Sender.Uin, out var sign) && DateTime.Today == sign.LastSign.Date)
            {
                builder.Text($"{messageEvent.Sender.Name}\n你今天已经签到过了");
            }
            else
            {
                sign = signList.Sign(messageEvent.Sender.Uin);
                SaveList();
                var random = new Random();
                var coin = random.Next(100);
                var exp = random.Next(50);
                var favor = random.Next(10);

                var info = GlobalScope.PermissionManager.GetUserInfo(messageEvent.Sender.Uin);
                info.Coin += coin;
                info.Exp += exp;
                info.Favor += favor;
                GlobalScope.PermissionManager.UpdateUserInfo(info);
                builder.Text($"{messageEvent.Sender.Name}\n签到成功\n" +
                    $"你是今天第 {signList.SignCountToday} 个签到的\n" +
                    $"{(sign.ContinuousSign > 1 ? $"你已连续签到 {sign.ContinuousSign} 天\n" : "")}" +
                    $"RC +{coin}\n" +
                    $"经验 +{exp}\n" +
                    $"好感度 +{favor}");
            }
            messageEvent.Reply(builder);
        }
    }

    internal class SignList
    {
        public Dictionary<uint, SignInfo> List { get; set; } = new();
        public DateTime DateTime { get; set; } = DateTime.Now;

        public uint SignCountToday { get; set; } = 0u;

        public void Flush()
        {
            List<SignInfo> temp = List.Values.ToList();
            temp.RemoveAll(x => DateTime.Today - x.LastSign > new TimeSpan(48, 0, 0));
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
}
