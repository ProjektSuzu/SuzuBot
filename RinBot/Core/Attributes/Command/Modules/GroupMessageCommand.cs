using RinBot.Core.Components;

namespace RinBot.Core.Attributes.Command.Modules
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class GroupMessageCommand : MessageCommand
    {
        public GroupMessageCommand(string name, string[] regexs, Permission permission = Permission.User, bool isRaw = false) : base(name, regexs, permission, isRaw) { }
        public GroupMessageCommand(string name, string regex, Permission permission = Permission.User, bool isRaw = false) : base(name, regex, permission, isRaw) { }
    }
}
