using System.Text.Json;
using Konata.Core;
using Konata.Core.Interfaces;
using Konata.Core.Interfaces.Api;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuzuBot.EventArgs;
using SuzuBot.Events;

namespace SuzuBot.Bots;
internal class BotManager : IHostedService
{
    private readonly ILogger<BotManager> _logger;
    private readonly EventBus _eventBus;
    private readonly IDirectoryContents _configs;
    private readonly List<Bot> _bots = new();

    public IReadOnlyCollection<Bot> Bots => _bots;

    public BotManager(ILogger<BotManager> logger, EventBus eventBus, IHostEnvironment environment)
    {
        _logger = logger;
        _eventBus = eventBus;
        _configs = environment.ContentRootFileProvider.GetDirectoryContents("configs");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("加载 Bot 配置文件");
        foreach (var configFile in _configs.Where(x => !x.IsDirectory && x.Name.EndsWith(".json")))
        {
            using var fs = new FileStream(configFile.PhysicalPath, FileMode.Open, FileAccess.ReadWrite);
            var botConfig = await JsonSerializer.DeserializeAsync<BotConfig>(fs);
            if (botConfig is null) continue;
            if (botConfig.KeyStore is not null)
            {
                var bot = BotFather.Create(botConfig.Config, botConfig.Device, botConfig.KeyStore);
                _bots.Add(bot);
            }
            else
            {
                var bot = BotFather.Create(
                    botConfig.Uin,
                    botConfig.Password,
                    out var config,
                    out var device,
                    out var keyStore);
                _bots.Add(bot);
                botConfig.Config = config;
                botConfig.Device = device;
                botConfig.KeyStore = keyStore;
                botConfig.Password = null;
                fs.Seek(0, SeekOrigin.Begin);
                await JsonSerializer.SerializeAsync(fs, botConfig);
            }
        }
        _logger.LogInformation("Bot 配置文件加载完成");
        if (!_bots.Any()) return;
        foreach (var bot in _bots)
        {
            _logger.LogInformation("登入 Bot {uin}", bot.Uin);
            bot.OnCaptcha += Bot_OnCaptcha;
            bot.OnLog += Bot_OnLog;
            bot.OnFriendMessage += Bot_OnFriendMessage;
            bot.OnGroupMessage += Bot_OnGroupMessage;
            await bot.Login();
        }

        var observer = Observer<SuzuEventArgs>.Create(x =>
        {
            if (x is not MessageEventArgs eventArgs) return Task.CompletedTask;
            _logger.LogInformation("{senderName}({senderUin})|{receiverName}({receiverUin}) => {message}",
                eventArgs.Sender.Name, eventArgs.Sender.Uin, eventArgs.Receiver.Name, eventArgs.Receiver.Uin, eventArgs.Chain.ToString());
            return Task.CompletedTask;
        });
        _eventBus.Subscribe(observer);
    }

    private void Bot_OnGroupMessage(Bot sender, Konata.Core.Events.Model.GroupMessageEvent args)
    {
        var messageEventArgs = new MessageEventArgs()
        {
            Bot = sender,
            EventBus = _eventBus,
            Message = args.Message
        };
        _eventBus.PublishEvent(messageEventArgs);
    }

    private void Bot_OnFriendMessage(Bot sender, Konata.Core.Events.Model.FriendMessageEvent args)
    {
        var messageEventArgs = new MessageEventArgs()
        {
            Bot = sender,
            EventBus = _eventBus,
            Message = args.Message
        };
        _eventBus.PublishEvent(messageEventArgs);
    }

    private void Bot_OnLog(Bot sender, Konata.Core.Events.LogEvent args)
    {
        var logLevel = (LogLevel)args.Level + 1;
        _logger.Log(logLevel, "Bot {uin}: {message}", sender.Uin, args.EventMessage);
    }

    private void Bot_OnCaptcha(Bot sender, Konata.Core.Events.Model.CaptchaEvent args)
    {
        _logger.LogWarning("Bot {uin} 登入验证", sender.Uin);
        switch (args.Type)
        {
            case Konata.Core.Events.Model.CaptchaEvent.CaptchaType.Slider:
                {
                    _logger.LogWarning("滑块验证: {url}", args.SliderUrl);
                    Console.Write("Auth: ");
                    string auth = Console.ReadLine();
                    sender.SubmitSliderTicket(auth);
                    break;
                }

            case Konata.Core.Events.Model.CaptchaEvent.CaptchaType.Sms:
                {
                    _logger.LogWarning("短信验证: {url}", args.Phone);
                    Console.Write("Auth: ");
                    string auth = Console.ReadLine();
                    sender.SubmitSmsCode(auth);
                    break;
                }

            default:
                {
                    _logger.LogError("未知验证类型");
                    break;
                }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var bot in _bots)
        {
            await bot.Logout();
            bot.Dispose();
        }

        _bots.Clear();
    }
}
