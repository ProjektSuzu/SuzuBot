using Konata.Core;
using Konata.Core.Common;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces;
using Konata.Core.Interfaces.Api;
using Newtonsoft.Json;
using NLog;
using RinBot.Core.Component.Message;

namespace RinBot.Core.KonataCore
{
    internal class KonataBot
    {
        #region Singleton
        private static KonataBot instance;
        public static KonataBot Instance
        {
            get
            {
                if (instance == null) instance = new();
                return instance;
            }
        }
        private KonataBot()
        {
            konataConfigDirectory = Path.Combine(Global.CONFIG_PATH, "Konata");
            if (!Directory.Exists(konataConfigDirectory)) Directory.CreateDirectory(konataConfigDirectory);

            konataDevicePath = Path.Combine(konataConfigDirectory, "device.json");
            konataKeyStorePath = Path.Combine(konataConfigDirectory, "keyStore.json");
        }
        #endregion
        public Bot Bot = null;

        private Logger Logger = LogManager.GetLogger("Konata");

        private readonly string konataConfigDirectory;
        private readonly string konataDevicePath;
        private readonly string konataKeyStorePath;

        private BotConfig GetBotConfig()
        {
            return new()
            {
                TryReconnect = true,
                HighwayChunkSize = 1048576,
                //CustomHost = "msfwifi.3g.qq.com:8080",
                Protocol = OicqProtocol.Android,
            };
        }

        private BotDevice GetBotDevice()
        {
            BotDevice device = null;
            if (File.Exists(konataDevicePath))
            {
                device = JsonConvert.DeserializeObject<BotDevice>(File.ReadAllText(konataDevicePath));
            }

            if (device == null)
            {
                device = BotDevice.Default();
                File.WriteAllTextAsync(konataDevicePath, JsonConvert.SerializeObject(device));
            }
            return device;
        }

        private BotKeyStore GetBotKeyStore()
        {
            BotKeyStore keyStore = null;
            if (File.Exists(konataKeyStorePath))
            {
                keyStore = JsonConvert.DeserializeObject<BotKeyStore>(File.ReadAllText(konataKeyStorePath));
            }

            if (keyStore == null)
            {
                Logger.Warn("Update keyStore before first-time use.");
                Console.Write("Account: ");
                string? account = Console.ReadLine();
                Console.Write("Password: ");
                string? password = Console.ReadLine();
                keyStore = UpdateBotKeyStore(new BotKeyStore(account, password));
                Console.WriteLine("Keystore updated.");
            }
            return keyStore;
        }

        private BotKeyStore UpdateBotKeyStore(BotKeyStore keyStore)
        {
            File.WriteAllTextAsync(konataKeyStorePath, JsonConvert.SerializeObject(keyStore));
            return keyStore;
        }

        public void InitializeBot()
        {
            if (Bot != null)
            {
                Logger.Warn($"Bot alive (U{Bot.Uin}), disposing.");
                Bot.Dispose();
            }

            Logger.Info("Bot Initializing.");

            BotConfig config = GetBotConfig();
            BotDevice device = GetBotDevice();
            BotKeyStore keyStore = GetBotKeyStore();

            Bot = BotFather.Create(
                config,
                device,
                keyStore
                );
            {
                Bot.OnLog += (s, e) => { Logger.Debug(e.EventMessage); };
                Bot.OnCaptcha += (s, e) =>
                {
                    switch (e.Type)
                    {
                        case CaptchaEvent.CaptchaType.Sms:
                            Logger.Warn($"Sms code required: {e.Phone}");
                            s!.SubmitSmsCode(Console.ReadLine());
                            break;

                        case CaptchaEvent.CaptchaType.Slider:
                            Logger.Warn($"Slider challenge required.\n{e.SliderUrl}");
                            s!.SubmitSliderTicket(Console.ReadLine());
                            break;

                        default:
                        case CaptchaEvent.CaptchaType.Unknown:
                            Logger.Warn($"Unknown captcha type.");
                            break;
                    }
                };

                Bot.OnBotOffline += (s, e) =>
                {
                    Logger.Warn($"Bot offline. {e.Type}");
                };
            }

            #region Konata
            Bot.OnGroupMessage += MessageProcessor.Instance.OnKonataGroupMessage;
            Bot.OnFriendMessage += MessageProcessor.Instance.OnKonataFriendMessage;
            Bot.OnGroupPoke += MessageProcessor.Instance.OnKonataGroupPoke;
            Bot.OnFriendPoke += MessageProcessor.Instance.OnKonataFriendPoke;
            #endregion

            Logger.Info("Bot initialization completed.");
            return;
        }

        public bool LoginBot()
        {
            bool result = Bot.Login().Result;
            if (result)
            {
                Logger.Info("Bot login success.");
                Logger.Info($"{Bot.GetGroupList(true).Result.Count} Group(s), {Bot.GetFriendList(true).Result.Count} Friend(s).");
            }
            else
                Logger.Fatal("Bot login failed.");

            return result;
        }
    }
}
