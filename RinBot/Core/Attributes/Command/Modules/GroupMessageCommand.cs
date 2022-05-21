using ProjektRin.Core.Components;

namespace ProjektRin.Core.Attributes.Command.Modules
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class GroupMessageCommand : MessageCommand
    {
        public GroupMessageCommand(string name, Permission permission, bool isRaw, string[] regexs) : base(name, permission, isRaw, regexs) { }
        public GroupMessageCommand(string name, Permission permission, string[] regexs) : base(name, permission, regexs) { }
        public GroupMessageCommand(string name, string[] regexs) : base(name, regexs) { }
    }
}
