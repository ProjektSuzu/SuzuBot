using System.CommandLine;
using System.Reflection;
using Lagrange.Core.Utility.Binary;
using Microsoft.Extensions.Logging;
using SuzuBot.Commands;
using SuzuBot.Commands.Attributes;
using SuzuBot.Hosting;

namespace SuzuBot.Services;

internal class CommandManager
{
    public RootCommand RootCommand { get; }
    public SuzuCommand[] Commands { get; }

    public CommandManager(Type[] moduleTypes, ILogger<CommandManager> logger)
    {
        RootCommand = [];
        RootCommand.Name = "suzubot";

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
                RootCommand.Add(command.InnerCommand);
            }
        }

        commands.Sort(
            (a, b) =>
            {
                var priority = a.RouteRule.Priority.CompareTo(b.RouteRule.Priority);
                return priority == 0 ? a.Id.CompareTo(b.Id) : priority;
            }
        );
        Commands = [.. commands];
    }

    public bool Match(RequestContext context)
    {
        context.ParseResult = RootCommand.Parse(context.Input);
        if (context.ParseResult.Errors.Count == 0)
        {
            context.Command = Commands
                .AsParallel()
                .Single(command =>
                    context.ParseResult.CommandResult.Command == command.InnerCommand
                );
            context.CommandPrefix = context.Command.RouteRule.Prefix;
            context.MessagePrefix &= context.CommandPrefix;
            return true;
        }

        var shortcut = Commands.FirstOrDefault(command => command.MatchShortcut(context));
        if (shortcut is not null)
        {
            context.Command = shortcut;
            context.ParseResult = shortcut.InnerCommand.Parse(context.Input);
            context.MessagePrefix &= context.CommandPrefix;
            context.UseShortcut = true;
            return true;
        }

        return false;
    }
}
