using System.CommandLine;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using SuzuBot.Commands.Attributes;
using SuzuBot.Hosting;

namespace SuzuBot.Commands;

internal class SuzuCommand
{
    public string Id { get; }
    public string Name { get; }
    public Type ModuleType { get; }
    public MethodInfo MethodInfo { get; }
    public Command InnerCommand { get; }
    public RouteRuleAttribute RouteRule { get; }
    public Symbol[] Symbols { get; }
    public (Regex Regex, string FormatString, Prefix Prefix)[] Shortcuts { get; }

    public SuzuCommand(Type module, MethodInfo method, CommandAttribute commandAttribute)
    {
        Id = $"{module.Name}.{method.Name}";
        Name = commandAttribute.Name;
        ModuleType = module;
        MethodInfo = method;
        InnerCommand = new Command(Id);
        InnerCommand.AddAlias(method.Name.ToLower());
        foreach (var alias in commandAttribute.Aliases)
            InnerCommand.AddAlias(alias);

        RouteRule = method.GetCustomAttribute<RouteRuleAttribute>() ?? new();

        List<Symbol> symbols = [];
        Delegate makeFunc(ParameterInfo parameter)
        {
            var funcType = typeof(Func<>).MakeGenericType(parameter.ParameterType);
            var constant = Expression.Constant(parameter.DefaultValue, parameter.ParameterType);
            var lambda = Expression.Lambda(funcType, constant);
            return lambda.Compile();
        }

        foreach (var parameter in method.GetParameters()[1..])
        {
            var optionAttribute = parameter.GetCustomAttribute<OptionAttribute>();
            if (optionAttribute is not null)
            {
                Option option;
                var optionType = typeof(Option<>).MakeGenericType(parameter.ParameterType);
                if (parameter.HasDefaultValue)
                    option = (Option)
                        Activator.CreateInstance(
                            optionType,
                            [$"--{parameter.Name}", makeFunc(parameter), null]
                        )!;
                else
                    option = (Option)
                        Activator.CreateInstance(optionType, [$"--{parameter.Name}", null])!;

                InnerCommand.Add(option);
                symbols.Add(option);
            }
            else
            {
                Argument argument;
                var argumentType = typeof(Argument<>).MakeGenericType(parameter.ParameterType);
                if (parameter.HasDefaultValue)
                    argument = (Argument)
                        Activator.CreateInstance(
                            argumentType,
                            [parameter.Name, makeFunc(parameter), null]
                        )!;
                else
                    argument = (Argument)
                        Activator.CreateInstance(argumentType, [parameter.Name, null])!;

                InnerCommand.Add(argument);
                symbols.Add(argument);
            }
        }

        Symbols = [.. symbols];
        List<(Regex, string, Prefix)> shortcuts = [];
        var shortcutAttributes = method.GetCustomAttributes<ShortcutAttribute>();
        foreach (var shortcutAttribute in shortcutAttributes)
        {
            var regex = new Regex(shortcutAttribute.Pattern);
            shortcuts.Add((regex, shortcutAttribute.FormatString, shortcutAttribute.Prefix));
        }

        Shortcuts = [.. shortcuts];
    }

    public Task ExecuteAsync(RequestContext context)
    {
        List<object> args = [context];
        foreach (var symbol in Symbols)
        {
            if (symbol is Option option)
            {
                var value = context.ParseResult!.GetValueForOption(option);
                if (value is null)
                    return Task.CompletedTask;
                else
                    args.Add(value);
            }
            else if (symbol is Argument argument)
            {
                var value = context.ParseResult!.GetValueForArgument(argument);
                if (value is null)
                    return Task.CompletedTask;
                else
                    args.Add(value);
            }
            else
                throw new NotImplementedException();
        }

        return Task.Run(async () =>
        {
            var instance = ActivatorUtilities.GetServiceOrCreateInstance(
                context.Services,
                ModuleType
            );
            var result = MethodInfo.Invoke(instance, [.. args]);
            if (result is Task task)
                await task;
        });
    }

    public bool MatchShortcut(RequestContext context)
    {
        foreach (var (regex, format, prefix) in Shortcuts)
        {
            if (regex.IsMatch(context.Input))
            {
                context.Input = $"{Id} {regex.Replace(context.Input, format)}";
                context.Prefix &= prefix;
                return true;
            }
        }

        return false;
    }
}
