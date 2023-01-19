using System.Reflection;
using System.Text.RegularExpressions;
using Konata.Core;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;

namespace SuzuBot.Core.Modules;
public class Command
{
    private readonly int _paramLength = 0;
    private readonly object?[] _defaultValues = Array.Empty<object?>();
    public BaseModule Module { get; set; }
    public MethodInfo MethodInfo { get; set; }
    public string Name { get; set; }
    public string FullName => $"{Module.Name}|{Name}";
    public bool IsEnabled => Module.IsEnabled;
    public AuthGroup AuthGroup { get; set; }
    public SourceType SourceType { get; set; }
    public bool WarnOnAuthFail { get; set; }
    public bool IgnorePrefix { get; set; }
    public byte Priority { get; set; }
    public Regex[] Regexes { get; set; }

    public Command(BaseModule module, MethodInfo methodInfo, string name)
    {
        Module = module;
        MethodInfo = methodInfo;
        Name = name;

        _paramLength = methodInfo.GetParameters().Length;
        if (_paramLength > 2)
            _defaultValues = methodInfo.GetParameters().Skip(2).Select(x => x.DefaultValue).ToArray();
    }

    public (bool Result, string[] Args) Match(MessageEventArgs eventArgs, string messageNoPrefix)
    {
        switch (SourceType)
        {
            case SourceType.Friend:
                if (eventArgs is not FriendMessageEventArgs) return (false, Array.Empty<string>()); break;
            case SourceType.Group:
                if (eventArgs is not GroupMessageEventArgs) return (false, Array.Empty<string>()); break;
            case SourceType.All:
                break;
        }
        if (Regexes.Length <= 0)
            return (true, Array.Empty<string>());

        foreach (var regex in Regexes)
        {
            var result = regex.Match(messageNoPrefix);
            if (result.Success)
            {
                if (result.Groups.Count > 1)
                {
                    string[] args = result.Groups.Values
                        .Select(x => x.Value)
                        .Skip(1)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .ToArray();
                    return (true, args);
                }
                else
                {
                    string[] args = regex.Split(messageNoPrefix, 2)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .ToArray();
                    return (true, args);
                }
            }
        }

        return (false, Array.Empty<string>());
    }
    public bool Auth(MessageEventArgs eventArgs)
    {
        if (eventArgs is GroupMessageEventArgs groupMessageEventArgs)
        {
            var groupAuth = groupMessageEventArgs.Member.Value.IsAdmin ? AuthGroup.Admin : AuthGroup.User;
            var userAuth = Module.Context.AuthManager.GetUserAuthGroup(eventArgs.Sender.Id);
            userAuth = userAuth < groupAuth ? userAuth : groupAuth;
            return userAuth <= AuthGroup;
        }
        else
        {
            var userAuth = Module.Context.AuthManager.GetUserAuthGroup(eventArgs.Sender.Id);
            return userAuth <= AuthGroup;
        }
    }
    public Task Invoke(Bot bot, MessageEventArgs eventArgs, string[] args)
    {
        object?[] param = new object[] { eventArgs, args };
        if (_paramLength > 2)
            param = param.Concat(_defaultValues).ToArray();
        return (Task)MethodInfo.Invoke(Module, param)!;
    }
}
