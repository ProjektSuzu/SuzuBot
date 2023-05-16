using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SuzuBot.Bots;
using SuzuBot.Events;
using SuzuBot.Modules;

namespace SuzuBot.Hosts;
internal class SuzuHost : IHost
{
    private readonly IHost _host;

    public IServiceProvider Services => _host.Services;
    public EventBus EventBus { get; init; }
    public BotManager BotManager { get; init; }
    public ModuleManager ModuleManager { get; init; }

    public SuzuHost(IHost host)
    {
        _host = host;
        EventBus = Services.GetRequiredService<EventBus>();
        BotManager = Services.GetRequiredService<BotManager>();
        ModuleManager = Services.GetRequiredService<ModuleManager>();
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _host.Dispose();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine(
            """
            ============================================================
            ███████╗██╗   ██╗███████╗██╗   ██╗██████╗  ██████╗ ████████╗
            ██╔════╝██║   ██║╚══███╔╝██║   ██║██╔══██╗██╔═══██╗╚══██╔══╝
            ███████╗██║   ██║  ███╔╝ ██║   ██║██████╔╝██║   ██║   ██║   
            ╚════██║██║   ██║ ███╔╝  ██║   ██║██╔══██╗██║   ██║   ██║   
            ███████║╚██████╔╝███████╗╚██████╔╝██████╔╝╚██████╔╝   ██║   
            ╚══════╝ ╚═════╝ ╚══════╝ ╚═════╝ ╚═════╝  ╚═════╝    ╚═╝   
            ============================================================
            """);

        await _host.StartAsync();
        await EventBus.StartAsync(cancellationToken);
        await BotManager.StartAsync(cancellationToken);
        await ModuleManager.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await EventBus.StopAsync(cancellationToken);
        await BotManager.StopAsync(cancellationToken);
        await ModuleManager.StopAsync(cancellationToken);
        await _host.StopAsync();
    }
}
