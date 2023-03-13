using System.Reactive.Subjects;
using Konata.Core;
using Konata.Core.Common;
using Konata.Core.Interfaces;
using Konata.Core.Interfaces.Api;
using Microsoft.Extensions.Logging;
using SuzuBot.Core.EventArgs;
using SuzuBot.Core.EventArgs.Bot;
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
    public static string ResourceDirectory { get; } = Path.Combine(BaseDirectory, "resources");

    private Subject<SuzuEventArgs> _subject = new();
    private ILogger _contextLogger;
    private ILogger _botLogger;
    public IObservable<SuzuEventArgs> EventChannel => _subject;
    public List<Bot> Bots { get; private set; } = new();
    public List<uint> BotUins { get; private set; } = new();
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
        foreach (var bot in Bots)
        {
            bot.OnFriendMessage += (s, e) =>
            {
                var args = new FriendMessageEventArgs()
                {
                    Bot = s,
                    Message = e.Message,
                };
                _subject.OnNext(args);
                _botLogger.LogInformation($"BOT{s.Uin} {args.Friend.Value.Name}({args.Friend.Value.Id}): {e.Chain}");
            };

            bot.OnGroupMessage += (s, e) =>
            {
                if (BotUins.Contains(e.MemberUin)) return;
                var args = new GroupMessageEventArgs()
                {
                    Bot = s,
                    Message = e.Message,
                };
                _subject.OnNext(args);
                _botLogger.LogInformation($"BOT{s.Uin} {e.Message.Receiver.Name}({e.Message.Receiver.Uin})|{e.Message.Sender.Name}({e.Message.Sender.Uin}): {e.Chain}");
            };

            bot.OnFriendPoke += (s, e) =>
            {
                if (e.FriendUin == bot.Uin) return;
                var args = new PokeEventArgs()
                {
                    Bot = s,
                    SenderId = e.FriendUin,
                    SubjectId = e.FriendUin,
                    ReceiverId = e.SelfUin,
                    PokeType = PokeType.Friend
                };
                _subject.OnNext(args);
                _botLogger.LogInformation($"BOT{s.Uin} {e.FriendUin} {e.ActionPrefix} {e.SelfUin} {e.ActionSuffix}");
            };

            bot.OnGroupPoke += (s, e) =>
            {
                if (e.OperatorUin == bot.Uin) return;
                var args = new PokeEventArgs()
                {
                    Bot = s,
                    SenderId = e.OperatorUin,
                    SubjectId = e.GroupUin,
                    ReceiverId = e.MemberUin,
                    PokeType = PokeType.Group
                };
                _subject.OnNext(args);
                _botLogger.LogInformation($"BOT{s.Uin} {e.GroupUin} {e.OperatorUin} {e.ActionPrefix} {e.MemberUin} {e.ActionSuffix}");
            };

            bot.OnGroupInvite += (s, e) =>
            {
                s.ApproveGroupInvitation(e.GroupUin, e.InviterUin, e.Token);
            };
        }
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
    private void OnBotOnline(Bot sender, Konata.Core.Events.Model.BotOnlineEvent args)
    {
        File.WriteAllText(sender.KeyStore.SerializeJsonString(), Path.Combine(ConfigDirectory, sender.Uin.ToString(), "keystore.json"));
    }
    public Context()
    {
        _botLogger = LogUtils.CreateLogger("Bot");
        _contextLogger = LogUtils.CreateLogger<Context>();

        var dirs = Directory.GetDirectories(ConfigDirectory);

        foreach (var dir in dirs)
        {
            BotConfig config = File.ReadAllText(Path.Combine(dir, "config.json"))
            .DeserializeJson<BotConfig>()!;
            BotDevice device = File.ReadAllText(Path.Combine(dir, "device.json"))
            .DeserializeJson<BotDevice>()!;
            BotKeyStore keyStore = File.ReadAllText(Path.Combine(dir, "keystore.json"))
            .DeserializeJson<BotKeyStore>()!;

            var bot = BotFather.Create(config, device, keyStore);

            bot.OnLog += OnLog;
            bot.OnCaptcha += OnCaptcha;
            bot.OnBotOnline += OnBotOnline;

            Bots.Add(bot);
            BotUins.Add(bot.Uin);
        }

        InitManagers();
    }

    public async Task<bool> StartAsync()
    {
        if (Bots.Any(x => x.IsOnline()))
        {
            RegisterEvents();
            return true;
        }
        else
        {
            return false;
        }
    }
}
