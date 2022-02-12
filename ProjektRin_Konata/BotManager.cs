using Konata.Core;
using Konata.Core.Events.Model;
using System.Text.Json;

namespace ProjektRin
{
    public class BotManager
    {
        private static BotManager _instance = new();
        private BotManager() { }
        public static BotManager Instance => _instance;

        private Bot _bot;
        public Bot Bot { get { return _bot; } }

        private CommandManager _commandManager = CommandManager.Instance;
        private static CommandLineInterface _cli = CommandLineInterface.Instance;
        private static string TAG = "Bot";

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
            if (File.Exists("device.json"))
            {
                return JsonSerializer.Deserialize
                    <BotDevice>(File.ReadAllText("device.json"));
            }
            else
            {
                var _botDevice = BotDevice.Default();
                {
                    var deviceJson = JsonSerializer.Serialize(_botDevice,
                        new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText("device.json", deviceJson);
                }
                return _botDevice;
            }
        }

        public BotKeyStore GetKeyStore()
        {
            if (File.Exists("keystore.json"))
            {
                return JsonSerializer.Deserialize
                    <BotKeyStore>(File.ReadAllText("keystore.json"));
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
            File.WriteAllText("keystore.json", deviceJson);
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
                _bot.OnLog += (s, e) => { _cli.Debug(TAG, e.EventMessage); };
                _bot.OnCaptcha += (s, e)
                    =>
                {
                    switch (e.Type)
                    {
                        case CaptchaEvent.CaptchaType.SMS:
                            _cli.Print(e.Phone);
                            ((Bot)s)!.SubmitSmsCode(Console.ReadLine());
                            break;

                        case CaptchaEvent.CaptchaType.Slider:
                            _cli.Print(e.SliderUrl);
                            ((Bot)s)!.SubmitSliderTicket(Console.ReadLine());
                            break;

                        default:
                        case CaptchaEvent.CaptchaType.Unknown:
                            break;
                    }
                };
                _bot.OnGroupMessage += _commandManager.GroupMessageEventListener;
            }

            _commandManager.LoadCommands();
            return _bot;
        }

        public bool LoginBot()
        {
            return _bot.Login().Result;
        }

    }   
}
