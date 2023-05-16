using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuzuBot.Bots;
using SuzuBot.Events;
using SuzuBot.Modules;

namespace SuzuBot.Hosts;
internal class SuzuHostBuilder : HostBuilder
{
    public SuzuHostBuilder()
    {
        this
            .ConfigureLogging(logging =>
            {
                logging
                .AddConsole();
            })
            .ConfigureServices(services =>
            {
                services
                .AddSingleton<EventBus>()
                .AddSingleton<BotManager>()
                .AddSingleton<ModuleManager>();
            });
    }
}
