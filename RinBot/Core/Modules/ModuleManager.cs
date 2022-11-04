using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message.Model;
using Microsoft.Extensions.Logging;
using RinBot.Common;
using RinBot.Common.Attributes;
using RinBot.Common.EventArgs.Messages;
using RinBot.Core.Databases.Tables;
using RinBot.Utils;

namespace RinBot.Core.Modules;
internal class ModuleManager : BaseManager
{
    private readonly ILogger _logger = LoggerUtils.LoggerFactory.CreateLogger<ModuleManager>();
    private Dictionary<string, BaseModule> _modules = new();
    private List<BaseCommand> _commands = new();
    private List<string> _prefixs = new();

    public ImmutableDictionary<string, BaseModule> Modules => _modules.ToImmutableDictionary();
    public ImmutableList<BaseCommand> Commands => _commands.ToImmutableList();

    public ModuleManager(Context context) : base(context)
    {
        _prefixs = new()
        {
            "/",
            "铃酱",
            "\\" + AtChain.Create(Context.Bot.Uin).ToString()
        };
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

        InvokeCommand(eventArgs);
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

        InvokeCommand(eventArgs);
    }

    public async Task InvokeCommand(MessageEventArgs eventArgs)
    {
        _logger.LogInformation(eventArgs.EventMessage);
        if (!_commands.Any()) return;

        var matches = _commands
            .Select(x => (x.Match(eventArgs), x).ToTuple())
            .Where(x => x.Item1.IsMatch == true)
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
            _logger.LogInformation($"{denied.Item2.FullName} Access Denied {eventArgs.SenderId}" +
                    $"{(eventArgs is GroupMessageEventArgs ? $"|{eventArgs.ReceiverId}" : "")}");
            await Record(eventArgs, denied.Item2, CommandExecuteResult.AuthFail);
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
            try
            {
                await result.Item2.Invoke(eventArgs, result.Item1.Arguments);
                _logger.LogInformation($"{result.Item2.FullName} Invoked By {eventArgs.SenderId}" +
                    $"{(eventArgs is GroupMessageEventArgs ? $"|{eventArgs.ReceiverId}" : "")}");
                await Record(eventArgs, result.Item2, CommandExecuteResult.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unhandled Exception Threw When Execute Command {result.Item2.FullName}");
                await Record(eventArgs, result.Item2, CommandExecuteResult.Error);
                string message = $"[ERROR]\n" +
                    $"出现了意料之外的错误Σ( ° △ °|||)\n" +
                    $"{ex.GetType()}: {ex.Message}";
                await eventArgs.Reply(message);
            }
        }
    }
    public Task Record(MessageEventArgs eventArgs, BaseCommand command, CommandExecuteResult result)
    {
        ExecutionRecord record = new()
        {
            Date = eventArgs.EventTime,
            Id = eventArgs.SenderId,
            Name = eventArgs.SenderName,
            GroupId = eventArgs.ReceiverId,
            GroupName = eventArgs.ReceiverName,
            Command = command.FullName,
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
        if (moduleAttr == null)
            return;

        _logger.LogInformation($"Register Module {moduleAttr.Name}({type.Name})");
        var module = (BaseModule)Activator.CreateInstance(type)!;
        module.Id = type.Name;
        module.Name = moduleAttr.Name;
        module.IsCritical = moduleAttr.IsCritical;
        module.Context = Context;
        module.ResourceDirPath = Path.Combine("resources", module.Id);

        foreach (var method in type.GetMethods())
        {
            var attrs = method.GetCustomAttributes<CommandAttribute>();
            if (!attrs.Any())
                continue;

            var param = method.GetParameters();
            if (param.Length != 2 &&
                !param[0].ParameterType.IsInstanceOfType(typeof(MessageEventArgs)) &&
                param[1].ParameterType != typeof(string[]))
                continue;

            foreach (var methodAttr in attrs)
            {
                _logger.LogInformation($"{module.Name}|Register Command {methodAttr.Name}({method.Name})");

                var regexes = ParseCommands(methodAttr, _prefixs.ToArray());

                var command = new BaseCommand()
                {
                    Module = module,
                    Method = method,
                    Name = methodAttr.Name,
                    Commands = regexes,
                    Priority = methodAttr.Priority,
                    AuthGroup = methodAttr.AuthGroup,
                    AuthFailWarning = methodAttr.AuthFailWarning,
                    HandlerType = methodAttr.HandlerType,
                    SourceType = methodAttr.SourceType,
                };

                _commands.Add(command);
                _commands.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
        }

        module.Init();
        module.Enable();
        _modules.Add(module.Id, module);
    }
    public void RegisterModule(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
            if (type.IsSubclassOf(typeof(BaseModule)))
                RegisterModule(type);
    }
    public Regex[] ParseCommands(CommandAttribute attribute, string[] prefixs)
    {
        if (attribute.MatchType.HasFlag(Common.Attributes.MatchType.NoPrefix))
        {
            return attribute.Commands.Select(cmd =>
            {
                switch (attribute.MatchType)
                {
                    case Common.Attributes.MatchType.Equal:
                        return new Regex($"^{cmd}$");
                    case Common.Attributes.MatchType.StartsWith:
                        return new Regex($"^{cmd}");
                    case Common.Attributes.MatchType.EndsWith:
                        return new Regex($"{cmd}$");
                    case Common.Attributes.MatchType.Contains:
                    case Common.Attributes.MatchType.Regex:
                    default:
                        return new Regex(cmd);
                }
            }).ToArray();
        }
        else
        {

            return attribute.Commands.SelectMany(cmd =>
            {
                return prefixs.Select(prefix =>
                {
                    switch (attribute.MatchType)
                    {
                        case Common.Attributes.MatchType.Equal:
                            return new Regex($"^{prefix}\\s*{cmd}$");
                        case Common.Attributes.MatchType.StartsWith:
                            return new Regex($"^{prefix}\\s*{cmd}");
                        case Common.Attributes.MatchType.EndsWith:
                            return new Regex($"{cmd}$");
                        case Common.Attributes.MatchType.Contains:
                        case Common.Attributes.MatchType.Regex:
                        default:
                            return new Regex(cmd);
                    }
                });

            }).ToArray();
            
        }
    }
}
