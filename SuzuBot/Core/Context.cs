using System.Reactive.Subjects;
using Konata.Core;
using Konata.Core.Common;
using Konata.Core.Interfaces;
using Konata.Core.Interfaces.Api;
using Microsoft.Extensions.Logging;
using SuzuBot.Core.EventArgs;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Manager;
using SuzuBot.Utils;
using LogLevel = Konata.Core.Events.LogLevel;

namespace SuzuBot.Core;
public class Context
{
    public static string BaseDirectory { get; } = AppContext.BaseDirectory;
    public static string ConfigDirectory { get; } = Path.Combine(BaseDirectory, "configs");
    public static string DatabaseDirectory { get; } = Path.Combine(BaseDirectory, "databases");

    private Subject<SuzuEventArgs> _subject = new();
    private ILogger _contextLogger;
    private ILogger _botLogger;
    public IObservable<SuzuEventArgs> EventChannel => _subject;
    public Bot Bot { get; private set; }
    public ModuleManager ModuleManager;
    public AuthManager AuthManager;
    public DatabaseManager DatabaseManager;

    private void InitManagers()
    {
        ModuleManager = new(this);
        AuthManager = new(this);
        DatabaseManager = new(this);
    }
    private void RegisterEvents()
    {
        Bot.OnFriendMessage += (s, e) =>
        {
            var args = new FriendMessageEventArgs()
            {
                Bot = s,
                Message = e.Message,
            };
            _subject.OnNext(args);
            _botLogger.LogInformation($"{args.Friend.Value.Name}({args.Friend.Value.Id}): {e.Chain}");
        };
        Bot.OnGroupMessage += (s, e) =>
        {
            if (e.MemberUin == Bot.Uin) return;
            var args = new GroupMessageEventArgs()
            {
                Bot = s,
                Message = e.Message,
            };
            _subject.OnNext(args);
            _botLogger.LogInformation($"{e.Message.Receiver.Name}({e.Message.Receiver.Uin})|{e.Message.Sender.Name}({e.Message.Sender.Uin}): {e.Chain}");
        };
    }
    private void UpdateKeyStore(BotKeyStore keyStore)
    {
        File.WriteAllBytes(Path.Combine(ConfigDirectory, "keystore.json"), keyStore.SerializeJsonByteArray());
    }
    private void OnCaptcha(Bot sender, Konata.Core.Events.Model.CaptchaEvent args)
    {
        switch (args.Type)
        {
            case Konata.Core.Events.Model.CaptchaEvent.CaptchaType.Slider:
                {
                    _contextLogger.LogWarning("Slider Captcha Required");
                    Console.WriteLine($"Url: {args.SliderUrl}");
                    Console.Write("Auth: ");
                    string auth = Console.ReadLine();
                    sender.SubmitSliderTicket(auth);
                    return;
                }
            case Konata.Core.Events.Model.CaptchaEvent.CaptchaType.Sms:
                {
                    _contextLogger.LogWarning("SmsCode Captcha Required");
                    Console.WriteLine($"Phone: {args.Phone}");
                    Console.Write("Auth: ");
                    string auth = Console.ReadLine();
                    sender.SubmitSmsCode(auth);
                    return;
                }
            case Konata.Core.Events.Model.CaptchaEvent.CaptchaType.Unknown:
            default:
                {
                    _contextLogger.LogError("Unknow Captcha");
                    return;
                }
        }
    }
    private void OnLog(Bot sender, Konata.Core.Events.LogEvent args)
    {
        switch (args.Level)
        {
            case LogLevel.Verbose: _botLogger.LogDebug(args.EventMessage); break;
            case LogLevel.Information: _botLogger.LogInformation(args.EventMessage); break;
            case LogLevel.Warning: _botLogger.LogWarning(args.EventMessage); break;
            case LogLevel.Exception: _botLogger.LogError(args.EventMessage); break;
            case LogLevel.Fatal: _botLogger.LogCritical(args.EventMessage); break;
        };
    }
    public Context()
    {
        _botLogger = LogUtils.CreateLogger("Bot");
        _contextLogger = LogUtils.CreateLogger<Context>();

        BotConfig config = File.ReadAllText(Path.Combine(ConfigDirectory, "config.json"))
            .DeserializeJson<BotConfig>();
        BotDevice device = File.ReadAllText(Path.Combine(ConfigDirectory, "device.json"))
            .DeserializeJson<BotDevice>();
        BotKeyStore keyStore = File.ReadAllText(Path.Combine(ConfigDirectory, "keystore.json"))
            .DeserializeJson<BotKeyStore>();

        Bot = BotFather.Create(config, device, keyStore);

        Bot.OnLog += OnLog;
        Bot.OnCaptcha += OnCaptcha;
        InitManagers();
    }
    public Context(string id, string password)
    {
        _botLogger = LogUtils.CreateLogger("Bot");
        _contextLogger = LogUtils.CreateLogger<Context>();
        Bot = BotFather.Create(
            id,
            password,
            out var config,
            out var device,
            out var keyStore,
            OicqProtocol.AndroidPad);

        File.WriteAllBytes(Path.Combine(ConfigDirectory, "config.json"), config.SerializeJsonByteArray());
        File.WriteAllBytes(Path.Combine(ConfigDirectory, "device.json"), device.SerializeJsonByteArray());
        File.WriteAllBytes(Path.Combine(ConfigDirectory, "keystore.json"), keyStore.SerializeJsonByteArray());

        Bot.OnLog += OnLog;
        Bot.OnCaptcha += OnCaptcha;
        InitManagers();
    }
    public async Task<bool> StartAsync()
    {
        if (await Bot.Login())
        {
            RegisterEvents();
            UpdateKeyStore(Bot.KeyStore);
            return true;
        }
        else
        {
            return false;
        }
    }
}
