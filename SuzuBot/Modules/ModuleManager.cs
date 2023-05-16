using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SuzuBot.Attributes;
using SuzuBot.EventArgs;
using SuzuBot.Events;
using SuzuBot.Extensions;

namespace SuzuBot.Modules;
internal class ModuleManager : IHostedService
{
    private readonly ILogger<ModuleManager> _logger;
    private readonly IServiceProvider _provider;
    private readonly EventBus _eventBus;
    private readonly List<BaseModule> _modules = new();
    private readonly List<Command> _commands = new();
    private readonly List<string> _prefixs = new()
    {
        "/",
        "#",
        "铃酱",
    };

    public ModuleManager(ILogger<ModuleManager> logger, EventBus eventBus, IServiceProvider provider)
    {
        _logger = logger;
        _eventBus = eventBus;
        _provider = provider;
    }

    public void LoadFromAssembly(Assembly assembly)
    {
        var types = assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(BaseModule)));
        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<SuzuModuleAttribute>();
            if (attr is null) continue;
            _logger.LogInformation("加载模块: {name}", attr.Name);
            var module = (BaseModule)Activator.CreateInstance(type, new object[] { _provider });
            foreach (var method in type.GetMethods())
            {
                var methodAttr = method.GetCustomAttribute<SuzuCommandAttribute>();
                if (methodAttr is null) continue;
                _logger.LogInformation("注册命令: {name}", methodAttr.Name);
                var command = new Command(module, method, methodAttr);
                _commands.Add(command);
            }
            _commands.Sort((a, b) => a.Priority - b.Priority);
            _modules.Add(module);
        }
    }

    public void AddPrefix(string prefix)
    {
        _prefixs.Add(prefix);
    }

    public async Task ProcessMessage(MessageEventArgs eventArgs)
    {
        bool hasPrefix = false;
        string message = eventArgs.Chain.ToString();
        foreach (var prefix in _prefixs) 
        {
            if (message.StartsWith(prefix))
            {
                hasPrefix = true;
                message = message.Substring(prefix.Length);
                break;
            }        
        }
        
        var commands = _commands
            .Where(x => !x.UsePrefix || (x.UsePrefix && hasPrefix))
            .Select(x => (x, x.Match(message)))
            .Where(x => x.Item2.Success);
        if (!commands.Any()) return;
        var success = commands.FirstOrDefault();
        _logger.LogInformation("{senderName}({senderUin})|{receiverName}({receiverUin}) 执行命令 {commandName}",
            eventArgs.Sender.Name, eventArgs.Sender.Uin, eventArgs.Receiver.Name, eventArgs.Receiver.Uin, success.x.Name);
        await success.x.Invoke(eventArgs, success.Item2.Collection!);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        LoadFromAssembly(Assembly.GetExecutingAssembly());
        var observer = Observer<SuzuEventArgs>.Create(async x =>
        {
            if (x is not MessageEventArgs eventArgs) return;
            await ProcessMessage(eventArgs);
        });
        _eventBus.Subscribe(observer);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _commands.Clear();
        _modules.Clear();
        return Task.CompletedTask;
    }
}
