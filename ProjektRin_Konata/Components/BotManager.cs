using Konata.Core;
using Konata.Core.Common;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using NLog;
using System.Text.Json;
using Konata.Core.Interfaces;


namespace ProjektRin.Components
{
    public class BotManager
    {
        private static readonly BotManager _instance = new();
        private BotManager() { }
        public static BotManager Instance => _instance;

        private Bot _bot;
        public Bot Bot => _bot;

        public static string rootPath = AppDomain.CurrentDomain.BaseDirectory;
        public static string resourcePath = Path.Combine(rootPath, "resources");

        private readonly CommandManager _commandManager = CommandManager.Instance;
        private static readonly string TAG = "Bot";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public BotConfig GetConfig()
        {
            return new BotConfig
            {
                EnableAudio = true,
                TryReconnect = true,
                HighwayChunkSize = 8192,
                CustomHost = "msfwifi.3g.qq.com:8080"
            };
        }

        public BotDevice GetDevice()
        {
            if (File.Exists(Path.Combine(rootPath, "device.json")))
            {
                return JsonSerializer.Deserialize
                    <BotDevice>(File.ReadAllText(Path.Combine(rootPath, "device.json")));
            }
            else
            {
                BotDevice? _botDevice = BotDevice.Default();
                {
                    string? deviceJson = JsonSerializer.Serialize(_botDevice,
                        new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(Path.Combine(rootPath, "device.json"), deviceJson);
                }
                return _botDevice;
            }
        }

        public BotKeyStore GetKeyStore()
        {
            if (File.Exists(rootPath + "/keystore.json"))
            {
                return JsonSerializer.Deserialize
                    <BotKeyStore>(File.ReadAllText(Path.Combine(rootPath, "keystore.json")));
            }
            else
            {
                Console.WriteLine("For first running, please " +
                              "type your account and password.");

                Console.Write("Account: ");
                string? account = Console.ReadLine();

                Console.Write("Password: ");
                string? password = Console.ReadLine();

                Console.WriteLine("Keystore Updated.");
                return UpdateKeyStore(new BotKeyStore(account, password));
            }
        }

        public BotKeyStore UpdateKeyStore(BotKeyStore botKeyStore)
        {
            string? deviceJson = JsonSerializer.Serialize(botKeyStore,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(rootPath, "keystore.json"), deviceJson);
            return botKeyStore;
        }

        //参考Kagami的写法   
        public Bot InitBot()
        {
            BotConfig _botConfig = GetConfig();
            BotDevice _botDevice = GetDevice();
            BotKeyStore _botKeyStore = GetKeyStore();

            //_bot = new Bot(
            //    _botConfig,
            //    _botDevice,
            //    _botKeyStore
            //    );
            _bot = BotFather.Create(
                _botConfig,
                _botDevice,
                _botKeyStore
            );
            {
                _bot.OnLog += (s, e) => { Logger.Trace(e.EventMessage); };
                _bot.OnCaptcha += (s, e)
                    =>
                {
                    switch (e.Type)
                    {
                        case CaptchaEvent.CaptchaType.Sms:
                            Logger.Info(e.Phone);
                            ((Bot)s)!.SubmitSmsCode(Console.ReadLine());
                            break;

                        case CaptchaEvent.CaptchaType.Slider:
                            Logger.Info(e.SliderUrl);
                            ((Bot)s)!.SubmitSliderTicket(Console.ReadLine());
                            break;

                        default:
                        case CaptchaEvent.CaptchaType.Unknown:
                            break;
                    }
                };
                _bot.OnGroupMessage += _commandManager.GroupCommandHandler;
                _bot.OnGroupPoke += _commandManager.GroupPokeEventHandler;
                _bot.OnBotOffline += RelayLogin;
                _bot.OnFriendRequest += (sender, args) => 
                    sender.ApproveFriendRequest(args.ReqUin, args.Token);
                _bot.OnGroupInvite += (sender, args) =>
                    sender.ApproveGroupInvitation(args.GroupUin, args.InviterUin, args.Token);
            }
            return _bot;
        }

        public void RelayLogin(object sender, BotOfflineEvent args)
        {
            if (args.Type == BotOfflineEvent.OfflineType.ServerKickOff)
            {
                Logger.Info("Admin logged in.\n" +
                            "Sleep 30s before relogin.");
                Thread.Sleep(30000);
                (sender as Bot).Login();
            }
            else
            {
                (sender as Bot).Login();
            }
        }

        public bool LoginBot()
        {
            return _bot.Login().Result;
        }

    }
}
