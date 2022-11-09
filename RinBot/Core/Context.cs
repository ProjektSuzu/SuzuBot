using System.Reflection;
using Konata.Core;
using Konata.Core.Common;
using Konata.Core.Interfaces;
using Konata.Core.Interfaces.Api;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RinBot.Core.Auth;
using RinBot.Core.Databases;
using RinBot.Core.Modules;
using RinBot.Utils;

#pragma warning disable CS8600, CS8618

namespace RinBot.Core;
internal class Context
{
    private readonly ILogger _contextLogger = LoggerUtils.LoggerFactory.CreateLogger<Context>();
    private readonly ILogger _botLogger = LoggerUtils.LoggerFactory.CreateLogger<Bot>();
    private readonly string _configPath = Path.Combine("configs", "config.json");
    private readonly string _devicePath = Path.Combine("configs", "device.json");
    private readonly string _keyStorePath = Path.Combine("configs", "keyStore.json");
    public readonly string BaseDirPath = AppContext.BaseDirectory;
    public Bot Bot { get; private set; }

    public AuthManager AuthManager { get; private set; }
    public DataBaseManager DataBaseManager { get; private set; }
    public ModuleManager ModuleManager { get; private set; }

    public Context()
    {
        BotConfig _config;
        BotDevice _device;
        BotKeyStore _keyStore;

        if (!File.Exists(_keyStorePath))
            throw new FileNotFoundException();
        else
            _keyStore = JsonConvert.DeserializeObject<BotKeyStore>(File.ReadAllText(_keyStorePath));

        if (!File.Exists(_configPath))
        {
            _config = BotConfig.Default();
            File.WriteAllText(_configPath, JsonConvert.SerializeObject(_config, Formatting.Indented));
        }
        else
            _config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(_configPath));

        if (!File.Exists(_devicePath))
        {
            _device = BotDevice.Default();
            File.WriteAllText(_devicePath, JsonConvert.SerializeObject(_device, Formatting.Indented));
        }
        else
            _device = JsonConvert.DeserializeObject<BotDevice>(File.ReadAllText(_devicePath));

        Bot = BotFather.Create(_config, _device, _keyStore);
        InitManagers();
        RegisterEvents();
    }
    public Context(string uin, string passwd)
    {
        Bot = BotFather.Create(uin, passwd, out var config, out var device, out var keyStore);

        File.WriteAllText(_configPath, JsonConvert.SerializeObject(config));
        File.WriteAllText(_devicePath, JsonConvert.SerializeObject(device));
        File.WriteAllText(_keyStorePath, JsonConvert.SerializeObject(keyStore));
        InitManagers();
        RegisterEvents();
    }

    private void InitManagers()
    {
        DataBaseManager = new(this);
        ModuleManager = new(this);
        AuthManager = new(this);
    }
    private void RegisterEvents()
    {
        Bot.OnLog += Bot_OnLog;
        Bot.OnCaptcha += Bot_OnCaptcha;
        Bot.OnFriendMessage += ModuleManager.PrivateMessageHandler;
        Bot.OnGroupMessage += ModuleManager.GroupMessageHandler;
    }
    
    private void Bot_OnCaptcha(Bot sender, Konata.Core.Events.Model.CaptchaEvent args)
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
                    sender.SubmitSliderTicket(auth);
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

    private void Bot_OnLog(Bot sender, Konata.Core.Events.LogEvent args)
    {
        string message = $"BOT{sender.Uin}|{args.Tag} {args.EventMessage}";
        switch (args.Level)
        {
            case Konata.Core.Events.LogLevel.Fatal:
                _botLogger.LogCritical(message);
                break;
            case Konata.Core.Events.LogLevel.Exception:
                _botLogger.LogError(message);
                break;
            case Konata.Core.Events.LogLevel.Warning:
                _botLogger.LogWarning(message);
                break;
            case Konata.Core.Events.LogLevel.Information:
                _botLogger.LogInformation(message);
                break;
            case Konata.Core.Events.LogLevel.Verbose:
                _botLogger.LogTrace(message);
                break;
        }
    }

    public async Task StartAsync()
    {
        if (Bot == null || Bot.IsOnline())
            return;

        _contextLogger.LogInformation("Register Modules");
        ModuleManager.RegisterModule(Assembly.GetExecutingAssembly());

        _contextLogger.LogInformation("Login Bot");
        if (await Bot.Login())
        {
            File.WriteAllText(_keyStorePath, JsonConvert.SerializeObject(Bot.KeyStore, Formatting.Indented));
            _contextLogger.LogInformation("Bot Login Success");
            foreach (var module in ModuleManager.Modules)
                module.Value.Enable();
        }
        else
        {
            _contextLogger.LogCritical("Bot Login Failed");
        }

        return;
    }
}
