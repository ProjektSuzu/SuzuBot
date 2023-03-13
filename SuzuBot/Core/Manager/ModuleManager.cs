using System.Diagnostics;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Konata.Core.Message.Model;
using Microsoft.Extensions.Logging;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;
using SuzuBot.Core.Tables;

namespace SuzuBot.Core.Manager;
public class ModuleManager : BaseManager
{
    private List<string> _prefixs;
    private List<BaseModule> _modules = new();
    private List<Command> _commands = new();

    public IReadOnlyList<BaseModule> Modules => _modules;
    public IReadOnlyList<Command> Commands => _commands;

    public ulong ExecuteCount { get; private set; } = 0L;
    public ulong ExceptionCount { get; private set; } = 0L;
    public Exception? LastException { get; private set; } = null;
    public long LastCommandCostMillisecond { get; private set; } = 0L;

    public ModuleManager(Context context) : base(context)
    {
        context.EventChannel
            .Where(x => x is MessageEventArgs)
            .Select(x => (MessageEventArgs)x)
            .Subscribe(async (args) => await OnMessage(args));

        _prefixs = new()
        {
            "/",
            "铃酱"
        };

        foreach (var uin in context.BotUins)
        {
            _prefixs.Add(AtChain.Create(uin).ToString());
        }

        ReloadModules();
    }

    public async Task OnMessage(MessageEventArgs eventArgs)
    {
        string plainText = eventArgs.Chain.ToString().Trim();
        IEnumerable<Command> cmds = _commands.Where(x => x.IsEnabled);
        if (eventArgs is GroupMessageEventArgs)
        {
            var info = Context.DatabaseManager.GetGroupInfo(eventArgs.Subject.Id);
            foreach (var module in info.Modules)
            {
                cmds = cmds.Where(x => x.Module.GetType().Name != module);
            }

            if (!info.WhiteListed)
            {
                cmds = cmds.Where(x => !x.Module.IsWhiteListOnly);
            }
        }
        else
        {
            cmds = cmds.Where(x => !x.Module.IsWhiteListOnly);
        }
        foreach (var prefix in _prefixs)
        {
            if (plainText.StartsWith(prefix))
            {
                plainText = plainText.Substring(prefix.Length).Trim();
                goto Match;
            }
        }

        cmds = cmds.Where(x => x.IgnorePrefix);
    Match:
        var results = cmds
        .Select(x =>
        {
            var result = x.Match(eventArgs, plainText);
            return (x, result.Result, result.Args);
        })
        .Where(x => x.Result);
        if (!results.Any())
            return;

        var result = results.FirstOrDefault();
        var cmd = result.x;
        string[] cmdArgs = result.Args;

        if (!cmd.Auth(eventArgs))
        {
            ExecuteCount++;
            Logger.LogWarning($"{cmd.FullName} Action Denied {eventArgs.Sender.Id}" +
                    $"{(eventArgs is GroupMessageEventArgs ? $"|{eventArgs.Subject.Id}" : "")}");
            RecordInvoke(eventArgs, cmd, 0, CommandExecuteResult.AuthFail);
            if (cmd.WarnOnAuthFail)
            {
                string message = $"[Auth]\n" +
                $"权限不足o(*≧д≦)o!!\n" +
                    $"{cmd.FullName} 需要权限组 {cmd.AuthGroup}\n" +
                    $"此事将被报告";
                eventArgs.Reply(message).Wait();
            }
            return;
        }

        Stopwatch stopwatch = new Stopwatch();
        try
        {
            Logger.LogInformation($"{cmd.FullName} Begin Invoke By {eventArgs.Sender.Id}" +
                    $"{(eventArgs is GroupMessageEventArgs ? $"|{eventArgs.Subject.Id}" : "")}");
            stopwatch.Start();
            await cmd.Invoke(eventArgs.Bot, eventArgs, cmdArgs);
            stopwatch.Stop();
            RecordInvoke(eventArgs, cmd, stopwatch.ElapsedMilliseconds, CommandExecuteResult.Success);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LastCommandCostMillisecond = stopwatch.ElapsedMilliseconds;
            Logger.LogError(ex, $"Unhandled Exception Threw When Execute Command {cmd.FullName}");
            ExceptionCount++;
            LastException = ex;
            RecordException(ex);
            RecordInvoke(eventArgs, cmd, stopwatch.ElapsedMilliseconds, CommandExecuteResult.Error);
            string message = $"[Error]\n" +
                    $"出现了意料之外的错误Σ( ° △ °|||)\n" +
                    $"{ex.GetType()}: {ex.Message}\n" +
                    $"如果这个问题频繁出现 请使用\n" +
                    $"/report 你想要反馈的内容\n" +
                    $"进行反馈";
            eventArgs.Reply(message).Wait();
        }
        finally
        {
            ExecuteCount++;
            long milliseconds = stopwatch.ElapsedMilliseconds;
            LastCommandCostMillisecond = milliseconds;
            Logger.LogInformation($"{cmd.FullName} Completed In {milliseconds} ms");
        }
    }

    public Task RecordException(Exception ex)
    {
        ExceptionRecord record = new ExceptionRecord()
        {
            Date = DateTime.UtcNow,
            Type = ex.GetType().Name,
            Message = ex.Message
        };
        return Context.DatabaseManager.Connection
            .InsertAsync(record);
    }

    public Task RecordInvoke(MessageEventArgs eventArgs, Command command, long milliseconds, CommandExecuteResult result)
    {
        ExecutionRecord record = new()
        {
            Date = eventArgs.EventTime,
            Id = eventArgs.Sender.Id,
            Name = eventArgs.Sender.Name,
            ReceiverId = eventArgs.Subject.Id,
            ReceiverName = eventArgs.Subject.Name,
            Command = command.FullName,
            TotalMillisecond = milliseconds,
            Message = eventArgs.Message.Chain.ToString(),
            Result = result
        };
        return Context.DatabaseManager.Connection
            .InsertAsync(record);
    }

    public void RegisterModule(Type type)
    {
        if (!type.IsSubclassOf(typeof(BaseModule)))
            return;

        var instance = (BaseModule)Activator.CreateInstance(type)!;
        instance.Context = Context;
        instance.Init();
        Logger.LogInformation($"Register Module {instance.Name}");
        foreach (var method in type.GetMethods())
        {
            var attrs = method.GetCustomAttributes<CommandAttribute>();
            if (attrs.Count() <= 0)
                continue;

            foreach (var attr in attrs)
            {
                RegisterCommand(instance, method, attr);
            }
        }

        instance.Enable();
        _modules.Add(instance);
    }

    public void RegisterCommand(BaseModule module, MethodInfo method, CommandAttribute attr)
    {
        var regexes = attr.Patterns
            .Select(x => new Regex(x, RegexOptions.Singleline | RegexOptions.Compiled))
            .ToArray();
        var cmd = new Command(module, method, attr.Name)
        {
            AuthGroup = attr.AuthGroup,
            Regexes = regexes,
            IgnorePrefix = attr.IgnorePrefix,
            SourceType = attr.SourceType,
            WarnOnAuthFail = attr.WarnOnAuthFail,
            Priority = attr.Priority
        };
        Logger.LogInformation($"Register Command {cmd.FullName}");
        _commands.Add(cmd);
        _commands.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    internal void ReloadModules()
    {
        foreach (var module in _modules)
        {
            module.Disable();
            module.Destory();
        }
        _commands.Clear();
        _modules.Clear();

        foreach (var type in Assembly.GetExecutingAssembly()
                     .GetTypes()
                     .Where(x => x.IsSubclassOf(typeof(BaseModule))))
        {
            RegisterModule(type);
        }
    }
}
