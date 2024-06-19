using System.Reflection;
using System.Text.Json;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuzuBot.Commands.Attributes;
using SuzuBot.Database;
using SuzuBot.Services;

namespace SuzuBot.Hosting;

internal class SuzuAppHostBuilder(string[]? args = default)
{
    private readonly HostApplicationBuilder _hostBuilder =
        new(new HostApplicationBuilderSettings { Args = args });
    public IHostEnvironment Environment => _hostBuilder.Environment;
    public IConfigurationManager Configuration => _hostBuilder.Configuration;
    public IServiceCollection Services => _hostBuilder.Services;
    public ILoggingBuilder Logging => _hostBuilder.Logging;
    public IMetricsBuilder Metrics => _hostBuilder.Metrics;

    public SuzuAppHost Build()
    {
        Services.AddSystemd();
        Services.AddHttpClient();

        Services.AddDbContext<SuzuDbContext>(options =>
            options.UseSqlite(
                connectionString: Configuration.GetConnectionString("SuzuBotDataBase")
            )
        );

        var modules = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Select(type => (type.GetCustomAttribute<ModuleAttribute>(), type))
            .Where(x => x.Item1 is not null);
        foreach ((ModuleAttribute? attr, Type type) in modules)
            Services.Add(new ServiceDescriptor(type, type, attr!.Lifetime));

        Services.AddSingleton(sp =>
            ActivatorUtilities.CreateInstance<CommandManager>(
                sp,
                [modules.Select(x => x.type).ToArray()]
            )
        );
        Services.AddSingleton<MessageCache>();
        Services.AddSingleton<BotMetrics>();

        BotConfig config =
            new()
            {
                AutoReconnect = true,
                AutoReLogin = true,
                GetOptimumServer = true,
                Protocol = Protocols.Linux,
            };
        BotDeviceInfo deviceInfo = File.Exists("configs/device.json")
            ? JsonSerializer.Deserialize<BotDeviceInfo>(File.ReadAllBytes("configs/device.json"))!
            : BotDeviceInfo.GenerateInfo();
        BotKeystore keystore = File.Exists("configs/keystore.json")
            ? JsonSerializer.Deserialize<BotKeystore>(File.ReadAllBytes("configs/keystore.json"))!
            : new BotKeystore();
        Services.AddSingleton(BotFactory.Create(config, deviceInfo, keystore));

        return new(_hostBuilder.Build());
    }
}
