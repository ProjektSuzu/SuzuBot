using Microsoft.Extensions.Hosting;
using SuzuBot.Hosts;

namespace SuzuBot;
public static class Program
{
    public static async Task Main()
    {
        var builder = new SuzuHostBuilder();
        var host = new SuzuHost(builder.Build());
        await host.StartAsync();
        await host.WaitForShutdownAsync();
    }
}
