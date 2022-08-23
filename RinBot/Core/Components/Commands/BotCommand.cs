using System.Reflection;

namespace RinBot.Core.Components.Commands
{
    internal class BotCommand
    {
        public string Name { get; protected set; }
        public string[] Tokens { get; protected set; }
        public UserPermission Permission { get; protected set; }
        public MethodInfo Method { get; protected set; }
        public string ParentId { get; protected set; }

        public BotCommand(string name, string[] funcNames, UserPermission permission, MethodInfo method, string parentId)
        {
            Name = name;
            Tokens = funcNames;
            Permission = permission;
            Method = method;
            ParentId = parentId;
        }
    }
}
