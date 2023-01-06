using System.Reflection;
using System.Text.RegularExpressions;
using Konata.Core;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.Contacts;
using SuzuBot.Core.EventArgs.Message;

namespace SuzuBot.Core.Modules;
public class Command
{
    public BaseModule Module { get; set; }
    public MethodInfo MethodInfo { get; set; }
    public string Name { get; set; }
    public string FullName => $"{Module.Name}|{Name}";
    public bool IsEnabled => Module.IsEnabled;
    public AuthGroup AuthGroup { get; set; }
    public bool WarnOnAuthFail { get; set; }
    public bool IgnorePrefix { get; set; }
    public byte Priority { get; set; }
    public Regex[] Regexes { get; set; }

    public Command(BaseModule module, MethodInfo methodInfo, string name)
    {
        Module = module;
        MethodInfo = methodInfo;
        Name = name;
    }

    public (bool Result, string[] Args) Match(MessageEventArgs eventArgs, string messageNoPrefix)
    {
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
                        .ToArray();
                    return (true, args);
                }
                else
                {
                    string[] args = regex.Split(messageNoPrefix, 2).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
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
        return (Task)MethodInfo.Invoke(Module, new object[] { bot, eventArgs, args })!;
    }
}
