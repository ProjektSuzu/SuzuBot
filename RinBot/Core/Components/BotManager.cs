using Konata.Core;
using Konata.Core.Common;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces;
using Konata.Core.Interfaces.Api;
using NLog;
using System.Text.Json;


namespace RinBot.Core.Components
{
    public class BotManager
    {
        #region 单例模式
        private static BotManager instance;
        private BotManager()
        {
            if (!Directory.Exists(configPath))
                Directory.CreateDirectory(configPath);
        }
        public static BotManager Instance
        {
            get
            {
                if (instance == null) instance = new();
                return instance;
            }
        }
        #endregion

        private Bot bot;
        public Bot Bot => bot;

        public static readonly string rootPath = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string configPath = Path.Combine(rootPath, "configs");
        public static readonly string resourcePath = Path.Combine(rootPath, "resources");

        public static readonly uint DevGroupUin = 644504300;

        private static readonly string TAG = "Bot";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public static bool AutoAccept = true;

        public BotConfig GetConfig()
        {
            return new BotConfig
            {
                EnableAudio = true,
                TryReconnect = true,
                HighwayChunkSize = 1024000,
                //CustomHost = "msfwifi.3g.qq.com:8080",
                Protocol = OicqProtocol.Android,
            };
        }

        public BotDevice? GetDevice()
        {
            if (File.Exists(Path.Combine(configPath, "device.json")))
            {
                return JsonSerializer.Deserialize
                    <BotDevice>(File.ReadAllText(Path.Combine(configPath, "device.json")));
            }
            else
            {
                BotDevice? _botDevice = BotDevice.Default();
                {
                    string? deviceJson = JsonSerializer.Serialize(_botDevice,
                        new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(Path.Combine(configPath, "device.json"), deviceJson);
                }
                return _botDevice;
            }
        }

        public BotKeyStore? GetKeyStore()
        {
            if (File.Exists(Path.Combine(configPath, "keystore.json")))
            {
                return JsonSerializer.Deserialize
                    <BotKeyStore>(File.ReadAllText(Path.Combine(configPath, "keystore.json")));
            }
            else
            {
                Console.WriteLine("第一次使用请先输入账号和密码");

                Console.Write("账号: ");
                string? account = Console.ReadLine();

                Console.Write("密码: ");
                string? password = Console.ReadLine();

                Console.WriteLine("密钥已更新.");
                return UpdateKeyStore(new BotKeyStore(account, password));
            }
        }

        public BotKeyStore UpdateKeyStore(BotKeyStore botKeyStore)
        {
            string? deviceJson = JsonSerializer.Serialize(botKeyStore,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(configPath, "keystore.json"), deviceJson);
            return botKeyStore;
        }

        //参考Kagami的写法   
        public Bot InitBot()
        {
            if (bot != null)
                bot.Dispose();

            BotConfig _botConfig = GetConfig();
            BotDevice _botDevice = GetDevice();
            BotKeyStore _botKeyStore = GetKeyStore();

            bot = BotFather.Create(
                _botConfig,
                _botDevice,
                _botKeyStore
            );
            {
                bot.OnLog += (s, e) => { Logger.Trace(e.EventMessage); };
                bot.OnCaptcha += (s, e)
                    =>
                {
                    switch (e.Type)
                    {
                        case CaptchaEvent.CaptchaType.Sms:
                            Logger.Info(e.Phone);
                            s!.SubmitSmsCode(Console.ReadLine());
                            break;

                        case CaptchaEvent.CaptchaType.Slider:
                            Logger.Info(e.SliderUrl);
                            s!.SubmitSliderTicket(Console.ReadLine());
                            break;

                        default:
                        case CaptchaEvent.CaptchaType.Unknown:
                            break;
                    }
                };


                bot.OnGroupMessage += (s, e) =>
                {
                    Logger.Debug($"{e.GroupName}({e.GroupUin})|{e.MemberCard}({e.MemberUin}):\n{e.Message.Chain.ToString()}");
                };

                bot.OnGroupPoke += (s, e) =>
                {
                    Logger.Debug($"{e.GroupUin}:{e.OperatorUin} {e.ActionPrefix} {e.MemberUin} {e.ActionSuffix}");
                };

                bot.OnGroupInvite += (s, e) =>
                {
                    Logger.Debug($"{e.InviterNick}({e.InviterUin}) 邀请进入群聊 {e.GroupName}({e.GroupUin})");
                    if (AutoAccept)
                    {
                        if (s.GetGroupMemberList(955578812).Result.FirstOrDefault(x => x.Uin == e.InviterUin, null) != null)
                        {
                            s.ApproveGroupInvitation(e.GroupUin, e.InviterUin, e.Token);
                            Logger.Debug($"已自动同意进入群聊 {e.GroupName}({e.GroupUin})");
                        }
                        else
                        {
                            s.DeclineGroupInvitation(e.GroupUin, e.InviterUin, e.Token);
                            s.SendFriendMessage(e.InviterUin, $"拒绝进入群聊\n请先加入 RinBot 认领群 (955578812)");
                            Logger.Debug($"已自动拒绝进入群聊 {e.GroupName}({e.GroupUin})");
                        }
                    }

                };

                bot.OnFriendRequest += (s, e) =>
                {
                    Logger.Debug($"{e.ReqNick}({e.ReqUin}) 请求添加好友");
                    if (AutoAccept)
                    {
                        s.ApproveFriendRequest(e.ReqUin, e.Token);
                        Logger.Debug($"已自动同意添加好友 {e.ReqNick}({e.ReqUin})");
                    }
                };

                bot.OnGroupMessage += (s, e) =>
                {
                    Task.Run(() =>
                    {
                        CommandManager.Instance.OnGroupMessageEvent(s, e);
                    });
                };
                bot.OnGroupPoke += (s, e) =>
                {
                    Task.Run(() =>
                    {
                        CommandManager.Instance.OnGroupPokeEvent(s, e);
                    });
                };

                bot.OnBotOffline += (s, e) =>
                {
                    if (e.Type == BotOfflineEvent.OfflineType.ServerKickOff)
                    {
                        Logger.Info("Bot kicked off.\n" +
                            "Sleep 30s before relogin.");
                        Thread.Sleep(30000);
                        (s as Bot).Login();
                    }
                };
            }
            return bot;
        }
    }
}
