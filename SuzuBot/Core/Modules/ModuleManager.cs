using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message.Model;
using Microsoft.Extensions.Logging;
using SuzuBot.Common;
using SuzuBot.Common.Attributes;
using SuzuBot.Common.EventArgs.Messages;
using SuzuBot.Core.Databases.Tables;
using SuzuBot.Utils;

#pragma warning disable CA2254

namespace SuzuBot.Core.Modules;
internal class ModuleManager : BaseManager
{
    private readonly ILogger _logger = LoggerUtils.LoggerFactory.CreateLogger<ModuleManager>();
    private readonly Dictionary<string, BaseModule> _modules = new();
    private readonly List<BaseCommand> _commands = new();
    private readonly List<string> _prefixs = new();

    public ImmutableDictionary<string, BaseModule> Modules => _modules.ToImmutableDictionary();
    public ImmutableList<BaseCommand> Commands => _commands.ToImmutableList();

    public ulong ExecuteCount { get; private set; } = 0L;
    public ulong ExceptionCount { get; private set; } = 0L;
    public Exception? LastException { get; private set; } = null;
    public long LastCommandCostMillisecond { get; private set; } = 0L;

    public ModuleManager(Context context) : base(context)
    {
        _prefixs = new()
        {
            "/",
            "铃酱",
            "\\" + AtChain.Create(Context.Bot.Uin).ToString()
        };
    }

    public void ClearModules()
    {
        _commands.Clear();
        foreach (var module in _modules)
            module.Value.Disable();
        _modules.Clear();
    }
    public void GroupMessageHandler(Bot bot, GroupMessageEvent messageEvent)
    {
        if (messageEvent.MemberUin == bot.Uin) return;
        GroupMessageEventArgs eventArgs = new()
        {
            EventTime = messageEvent.EventTime,
            EventMessage = $"{messageEvent.MemberCard}({messageEvent.MemberUin})|{messageEvent.GroupName}({messageEvent.GroupUin})" +
            $"\n{messageEvent.Chain}",
            Bot = bot,
            Message = messageEvent.Message,
        };

        _ = InvokeCommand(eventArgs);
    }
    public void PrivateMessageHandler(Bot bot, FriendMessageEvent messageEvent)
    {
        PrivateMessageEventArgs eventArgs = new()
        {
            EventTime = messageEvent.EventTime,
            EventMessage = $"{messageEvent.FriendUin}\n{messageEvent.Chain}",
            Bot = bot,
            Message = messageEvent.Message,
        };

        _ = InvokeCommand(eventArgs);
    }
    public async Task InvokeCommand(MessageEventArgs eventArgs)
    {
        _logger.LogInformation(eventArgs.EventMessage);
        if (!_commands.Any()) return;

        var matches = _commands
            .Select(x => (x.Match(eventArgs), x).ToTuple())
            .Where(x => x.Item1.IsMatch == true)
            .OrderBy(x => x.Item2.Priority)
            .ToArray();
        if (!matches.Any()) return;

        var lowestPriority = matches
            .Where(x => x.Item2.HandlerType == HandlerType.Block)
            .FirstOrDefault()
            ?.Item2.Priority
            ?? byte.MaxValue;

        matches = matches
            .TakeWhile(x => x.Item2.Priority <= lowestPriority)
            .ToArray();

        var auths = matches
            .GroupBy(x => x.Item2.Auth(eventArgs))
            .ToArray();

        var authDenieds = auths
            .SingleOrDefault(x => x.Key == false)
            ?.Where(x => x.Item2.AuthFailWarning)
            .ToArray()
            ?? Array.Empty<Tuple<(bool IsMatch, string[] Arguments), BaseCommand>>();

        foreach (var denied in authDenieds)
        {
            _logger.LogInformation($"{denied.Item2.FullName} Access Denied {eventArgs.SenderId}{(eventArgs is GroupMessageEventArgs ? $"|{eventArgs.ReceiverId}" : "")}");
            await RecordInvoke(eventArgs, denied.Item2, 0L, CommandExecuteResult.AuthFail);
            string message = $"[Auth]\n" +
                $"权限不足o(*≧д≦)o!!\n" +
                    $"{denied.Item2.Module.Name}|{denied.Item2.Name} 需要权限组 {denied.Item2.AuthGroup}\n" +
                    $"此事将被报告";
            await eventArgs.Reply(message);
        }

        matches = auths.SingleOrDefault(x => x.Key == true)
            ?.ToArray() ?? Array.Empty<Tuple<(bool IsMatch, string[] Arguments), BaseCommand>>();

        foreach (var result in matches)
        {
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                var args = result.Item1.Arguments.SelectMany(x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray();
                _logger.LogInformation($"{result.Item2.FullName} Begin Invoke By {eventArgs.SenderId}" +
                    $"{(eventArgs is GroupMessageEventArgs ? $"|{eventArgs.ReceiverId}" : "")}");
                stopwatch.Start();
                await result.Item2.Invoke(eventArgs, args);
                stopwatch.Stop();
                LastCommandCostMillisecond = stopwatch.ElapsedMilliseconds;
                _logger.LogInformation($"{result.Item2.FullName} Invoke Completed Cost {stopwatch.ElapsedMilliseconds} ms");
                await RecordInvoke(eventArgs, result.Item2, stopwatch.ElapsedMilliseconds, CommandExecuteResult.Success);
                ExecuteCount++;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LastCommandCostMillisecond = stopwatch.ElapsedMilliseconds;
                ExceptionCount++;
                LastException = ex;
                _logger.LogError(ex, $"Unhandled Exception Threw When Execute Command {result.Item2.FullName}");
                await RecordException(ex);
                await RecordInvoke(eventArgs, result.Item2, stopwatch.ElapsedMilliseconds, CommandExecuteResult.Error);
                string message = $"[Error]\n" +
                    $"出现了意料之外的错误Σ( ° △ °|||)\n" +
                    $"{ex.GetType()}";
                await eventArgs.Reply(message);
            }
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
        return Context.DataBaseManager.Connection
            .InsertAsync(record);
    }
    public Task RecordInvoke(MessageEventArgs eventArgs, BaseCommand command, long milliseconds, CommandExecuteResult result)
    {
        ExecutionRecord record = new()
        {
            Date = eventArgs.EventTime,
            Id = eventArgs.SenderId,
            Name = eventArgs.SenderName,
            ReceiverId = eventArgs.ReceiverId,
            ReceiverName = eventArgs.ReceiverName,
            Command = command.FullName,
            TotalMillisecond = milliseconds,
            Message = eventArgs.Chains.ToString(),
            Result = result
        };
        return Context.DataBaseManager.Connection
            .InsertAsync(record);
    }
    public void RegisterModule(Type type)
    {
        if (!type.IsSubclassOf(typeof(BaseModule)))
            return;

        var moduleAttr = type.GetCustomAttribute<ModuleAttribute>();
        if (moduleAttr is null)
            return;

        _logger.LogInformation($"Register Module {moduleAttr.Name}({type.Name})");
        var module = (BaseModule)Activator.CreateInstance(type)!;
        module.Id = type.Name;
        module.Name = moduleAttr.Name;
        module.IsCritical = moduleAttr.IsCritical;
        module.Context = Context;
        module.ResourceDirPath = Path.Combine(Context.BaseDirPath, "resources", module.Id);

        foreach (var method in type.GetMethods())
        {
            var attrs = method.GetCustomAttributes<CommandAttribute>();
            if (!attrs.Any())
                continue;

            foreach (var methodAttr in attrs)
            {
                _logger.LogInformation($"{module.Name}|Register Command {methodAttr.Name}({method.Name})");

                var regexes = ParseCommands(methodAttr, _prefixs.ToArray());
                var command = new BaseCommand(module, method, methodAttr, regexes);

                _commands.Add(command);
                _commands.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
        }

        module.Init();
        _modules.Add(module.Id, module);
    }
    public void RegisterModule(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
            if (type.IsSubclassOf(typeof(BaseModule)))
                RegisterModule(type);
    }
    public static Regex[] ParseCommands(CommandAttribute attribute, string[] prefixs)
    {
        if (attribute.MatchType.HasFlag(Common.Attributes.MatchType.NoPrefix))
        {
            return attribute.Commands.Select(cmd =>
            {
                return attribute.MatchType switch
                {
                    Common.Attributes.MatchType.Equal => new Regex($"^{cmd}$"),
                    Common.Attributes.MatchType.StartsWith => new Regex($"^{cmd}\\s?"),
                    Common.Attributes.MatchType.EndsWith => new Regex($"{cmd}$"),
                    _ => new Regex(cmd),
                };
            }).ToArray();
        }
        else
        {

            return attribute.Commands.SelectMany(cmd =>
            {
                return prefixs.Select(prefix =>
                {
                    return attribute.MatchType switch
                    {
                        Common.Attributes.MatchType.Equal => new Regex($"^{prefix}\\s?{cmd}$"),
                        Common.Attributes.MatchType.StartsWith => new Regex($"^{prefix}\\s?{cmd}\\s?"),
                        Common.Attributes.MatchType.EndsWith => new Regex($"{cmd}$"),
                        _ => new Regex(cmd),
                    };
                });

            }).ToArray();

        }
    }
}
