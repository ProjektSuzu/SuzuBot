using System.Reflection;
using System.Text.RegularExpressions;
using SuzuBot.Attributes;
using SuzuBot.EventArgs;
using SuzuBot.Extensions;

namespace SuzuBot.Modules;
internal class Command
{
    private readonly BaseModule _module;
    private readonly MethodInfo _method;
    private readonly Regex _regex;

    public string Name { get; init; }
    public byte Priority { get; init; }
    public bool UsePrefix { get; init; }

    public Command(BaseModule module, MethodInfo methodInfo, SuzuCommandAttribute attr)
    {
        Name = attr.Name;
        Priority = attr.Priority;
        UsePrefix = attr.UsePrefix;

        _method = methodInfo;
        _module = module;
        _regex = new Regex(attr.Pattern, RegexOptions.Compiled);
    }

    public (bool Success, GroupCollection? Collection) Match(string message)
    {
        var match = _regex.Match(message);
        if (match.Success) return (true, match.Groups);
        else return (false, null);
    }

    public async Task Invoke(MessageEventArgs eventArgs, GroupCollection collection)
    {
        if (_method.Invoke(_module, new object[] { eventArgs, collection }) is Task task)
            await task;
    }
}
