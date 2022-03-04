using Konata.Core;
using Konata.Core.Common;
using Konata.Core.Events.Model;
using NLog;
using System.Text.Json;

namespace ProjektRin.System
{
    public class BotManager
    {
        private static BotManager _instance = new();
        private BotManager() { }
        public static BotManager Instance => _instance;

        private Bot _bot;
        public Bot Bot { get { return _bot; } }

        public static string rootPath = AppDomain.CurrentDomain.BaseDirectory;
        public static string resourcePath = Path.Combine(rootPath, "resources");

        private CommandManager _commandManager = CommandManager.Instance;
        private static string TAG = "Bot";
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
                var _botDevice = BotDevice.Default();
                {
                    var deviceJson = JsonSerializer.Serialize(_botDevice,
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
                var account = Console.ReadLine();

                Console.Write("Password: ");
                var password = Console.ReadLine();

                Console.WriteLine("Keystore Updated.");
                return UpdateKeyStore(new BotKeyStore(account, password));
            }
        }

        public BotKeyStore UpdateKeyStore(BotKeyStore botKeyStore)
        {
            var deviceJson = JsonSerializer.Serialize(botKeyStore,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(rootPath, "device.json"), deviceJson);
            return botKeyStore;
        }

        //参考Kagami的写法
        public Bot InitBot()
        {
            BotConfig _botConfig = GetConfig();
            BotDevice _botDevice = GetDevice();
            BotKeyStore _botKeyStore = GetKeyStore();

            _bot = new Bot(
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
                        case CaptchaEvent.CaptchaType.SMS:
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
                _bot.OnGroupMessage += _commandManager.GroupMessageEventHandler;
            }
            return _bot;
        }

        public bool LoginBot()
        {
            return _bot.Login().Result;
        }

    }
}
