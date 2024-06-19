using System.Text.Json;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuzuBot.Database;
using SuzuBot.Services;
using Timer = System.Timers.Timer;

namespace SuzuBot.Hosting;

internal class SuzuAppHost : IHost
{
    private readonly ILogger _logger;
    private readonly IHost _host;
    private readonly Timer _timer;
    private BotContext? _bot;
    private RequestDelegate? _app;
    private List<Func<RequestDelegate, RequestDelegate>> _middleware = [];
    public IServiceProvider Services => _host.Services;

    public SuzuAppHost(IHost host)
    {
        Directory.CreateDirectory("configs");
        Directory.CreateDirectory("logs");
        Directory.CreateDirectory("resources");

        _host = host;
        _logger = host.Services.GetRequiredService<ILogger<SuzuAppHost>>();
        _timer = new()
        {
            AutoReset = true,
            Enabled = true,
            Interval = TimeSpan.FromDays(14).TotalMilliseconds,
        };

        _ = Services.GetRequiredService<CommandManager>();
        _ = Services.GetRequiredService<MessageCache>();
        _ = Services.GetRequiredService<BotMetrics>();
        Services.GetRequiredService<SuzuDbContext>().Database.EnsureCreated();
    }

    private void SaveLoginInfo()
    {
        if (_bot is null)
            return;

        File.WriteAllBytes(
            "configs/deviceInfo.json",
            JsonSerializer.SerializeToUtf8Bytes(_bot.UpdateDeviceInfo())
        );
        File.WriteAllBytes(
            "configs/keystore.json",
            JsonSerializer.SerializeToUtf8Bytes(_bot.UpdateKeystore())
        );
    }

    private async Task StartBot(CancellationToken cancellationToken = default)
    {
        if (_bot is not null)
        {
            _logger.LogWarning("Bot is already running.");
            return;
        }

        _bot = Services.GetRequiredService<BotContext>();
        _bot.Invoker.OnBotOnlineEvent += (_, _) => SaveLoginInfo();
        _bot.Invoker.OnBotOfflineEvent += (_, _) =>
        {
            StopBot().Wait();
            StartBot().Wait();
        };
        _bot.Invoker.OnBotLogEvent += (_, e) =>
            _logger.Log((LogLevel)e.Level, "[{}] {}", e.Tag, e.EventMessage);
        _bot.Invoker.OnGroupMessageReceived += (s, e) =>
        {
            var ctx = new RequestContext(Services, s, e.Chain);
            _ = _app(ctx);
        };

        if (_bot.BotUin != 0)
        {
            _logger.LogInformation("Login by FastLogin");
            if (await _bot.LoginByPassword().WaitAsync(cancellationToken))
                return;
            else
                _logger.LogError("Failed to login by FastLogin");
        }

        _logger.LogInformation("Login by QRCode");
        var qrResult = await _bot.FetchQrCode().WaitAsync(cancellationToken);
        if (qrResult is null)
        {
            _logger.LogError("Failed to fetch QRCode");
            return;
        }

        (string url, byte[] qrCode) = qrResult.Value;
        File.WriteAllBytes("qrCode.png", qrCode);
        _logger.LogInformation("QRCode saved to qrCode.png");
        _logger.LogInformation("Url: {url}", url);
        await _bot.LoginByQrCode().WaitAsync(cancellationToken);
    }

    private Task StopBot(CancellationToken cancellationToken = default)
    {
        _bot?.Dispose();
        _bot = null;
        return Task.CompletedTask;
    }

    public SuzuAppHost Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        _middleware.Add(middleware);
        return this;
    }

    public SuzuAppHost Use(Func<RequestContext, Func<RequestContext, Task>, Task> func)
    {
        return Use(next => context => func(context, next.Invoke));
    }

    public void Dispose()
    {
        _host.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_app is null)
        {
            _app = static _ => Task.CompletedTask;
            for (int i = _middleware.Count - 1; i >= 0; i--)
                _app = _middleware[i](_app);
        }

        await StartBot(cancellationToken);
        await _host.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await StopBot(cancellationToken);
        await _host.StopAsync(cancellationToken);
    }
}
