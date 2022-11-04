using System.Reflection;
using System.Text.RegularExpressions;
using RinBot.Common.EventArgs.Messages;

namespace RinBot.Common;

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
        if (eventArgs is GroupMessageEventArgs groupMessage)
        {
            return Module.Context.AuthManager.GetMemberAuth(groupMessage.SenderId, groupMessage.ReceiverId) >= Priority;
        }
        else if (eventArgs is PrivateMessageEventArgs privateMessage)
        {
            return Module.Context.AuthManager.GetUserAuth(privateMessage.SenderId) >= Priority;
        }
        else
        {
            return true;
        }
    }
    public Task Invoke(MessageEventArgs eventArgs, string[] args)
    {
        if (Method.ReturnType == typeof(Task))
            return (Task)Method.Invoke(Module, new object[] { eventArgs, args })!;
        else
        {
            _ = Method.Invoke(Module, new object[] { eventArgs, args });
            return Task.CompletedTask;
        }
    }
}
