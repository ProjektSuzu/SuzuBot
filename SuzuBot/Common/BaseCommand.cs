using System.Reflection;
using System.Text.RegularExpressions;
using SuzuBot.Common.Attributes;
using SuzuBot.Common.EventArgs.Messages;

namespace SuzuBot.Common;

public enum HandlerType
{
    Pass,
    Block
}

[Flags]
public enum SourceType
{
    Private = 1,
    Group = 2,
    All = Private | Group
}

internal class BaseCommand
{
    private readonly int _paramLength = 0;
    private readonly object?[] _defaultValues = Array.Empty<object?>();
    public BaseModule Module { get; set; }
    public MethodInfo Method { get; set; }
    public string Name { get; set; }
    public string FullName => $"{Module.Name}|{Name}";
    public byte Priority { get; set; }
    public bool AuthFailWarning { get; set; }
    public string AuthGroup { get; set; }
    public Regex[] Commands { get; set; }
    public HandlerType HandlerType { get; set; }
    public SourceType SourceType { get; set; }

    public BaseCommand(BaseModule module, MethodInfo method, CommandAttribute methodAttr, Regex[] regexes)
    {
        Module = module;
        Method = method;
        Name = methodAttr.Name;
        Commands = regexes;
        Priority = methodAttr.Priority;
        AuthGroup = methodAttr.AuthGroup;
        AuthFailWarning = methodAttr.AuthFailWarning;
        HandlerType = methodAttr.HandlerType;
        SourceType = methodAttr.SourceType;

        _paramLength = method.GetParameters().Length;
        if (_paramLength > 2)
            _defaultValues = Method.GetParameters().Skip(2).Select(x => x.DefaultValue).ToArray();
    }

    public (bool IsMatch, string[] Arguments) Match(MessageEventArgs eventArgs)
    {
        switch (SourceType)
        {
            case SourceType.All:
                break;
            case SourceType.Group:
                if (eventArgs is not GroupMessageEventArgs)
                    return (false, Array.Empty<string>());
                break;
            case SourceType.Private:
                if (eventArgs is not PrivateMessageEventArgs)
                    return (false, Array.Empty<string>());
                break;
        }

        if (!Commands.Any())
            return (true, Array.Empty<string>());

        string content = eventArgs.Chains.ToString();
        foreach (var command in Commands)
        {
            if (command.IsMatch(content))
            {
                string[] args = command.Split(content, 2);
                return (true, args);
            }
        }

        return (false, Array.Empty<string>());
    }
    public bool Auth(MessageEventArgs eventArgs)
    {
        var authPriority = Module.Context.AuthManager.GetAuthGroupPriority(AuthGroup);
        if (eventArgs is GroupMessageEventArgs groupMessage)
        {
            return Module.Context.AuthManager.GetMemberAuth(groupMessage.SenderId, groupMessage.ReceiverId) >= authPriority;
        }
        else if (eventArgs is PrivateMessageEventArgs privateMessage)
        {
            return Module.Context.AuthManager.GetUserAuth(privateMessage.SenderId) >= authPriority;
        }
        else
        {
            return true;
        }
    }
    public Task Invoke(MessageEventArgs eventArgs, string[] args)
    {
        args = args.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        object?[] param = new object[] { eventArgs, args };
        if (_paramLength <= 2)
        {
            param = param.Take(.._paramLength).ToArray();
        }
        else
        {
            param = param.Concat(_defaultValues).ToArray();
        }
        if (Method.ReturnType == typeof(Task))
            return (Task)Method.Invoke(Module, param)!;
        else
        {
            _ = Method.Invoke(Module, param);
            return Task.CompletedTask;
        }
    }
}
