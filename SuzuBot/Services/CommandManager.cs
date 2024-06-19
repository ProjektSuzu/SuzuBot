using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.Logging;
using SuzuBot.Commands;
using SuzuBot.Commands.Attributes;
using SuzuBot.Hosting;

namespace SuzuBot.Services;

internal class CommandManager
{
    private readonly RootCommand _rootCommand;
    private readonly SuzuCommand[] _commands;

    public CommandManager(Type[] moduleTypes, ILogger<CommandManager> logger)
    {
        _rootCommand = [];
        _rootCommand.Name = "suzubot";

        List<SuzuCommand> commands = [];
        foreach (var moduleType in moduleTypes)
        {
            var moduleAttribute =
                moduleType.GetCustomAttribute<ModuleAttribute>()
                ?? throw new InvalidOperationException(
                    $"{moduleType.Name} must have a ModuleAttribute."
                );
            Directory.CreateDirectory(Path.Combine("resources", moduleType.Name));
            logger.LogInformation("Registering module {}", moduleAttribute.Name);
            foreach (var method in moduleType.GetMethods())
            {
                var commandAttribute = method.GetCustomAttribute<CommandAttribute>();
                if (commandAttribute is null)
                    continue;

                logger.LogInformation("Registering command {}", commandAttribute.Name);
                var command = new SuzuCommand(moduleType, method, commandAttribute);
                commands.Add(command);
                _rootCommand.Add(command.InnerCommand);
            }
        }

        commands.Sort(
            (a, b) =>
            {
                var priority = a.RouteRule.Priority.CompareTo(b.RouteRule.Priority);
                return priority == 0 ? a.Id.CompareTo(b.Id) : priority;
            }
        );
        _commands = [.. commands];
    }

    public bool Match(RequestContext context)
    {
        context.ParseResult = _rootCommand.Parse(context.Input);
        if (context.ParseResult.Errors.Count == 0)
        {
            context.Command = _commands
                .AsParallel()
                .Single(command =>
                    context.ParseResult.CommandResult.Command == command.InnerCommand
                );
            context.Prefix &= context.Command.RouteRule.Prefix;
            return true;
        }

        var shortcut = _commands
            .AsParallel()
            .FirstOrDefault(command => command.MatchShortcut(context));
        if (shortcut is not null)
        {
            context.Command = shortcut;
            context.ParseResult = shortcut.InnerCommand.Parse(context.Input);
            context.UseShortcut = true;
            return true;
        }

        return false;
    }
}
