using System.Reflection;

namespace RinBot.Core.Components.Commands
{
    internal class Command
    {
        public string Name { get; protected set; }
        public string[] FuncNames { get; protected set; }
        public Permission Permission { get; protected set; }

        public MethodInfo Method { get; protected set; }
        public string ParentId { get; protected set; }

        public Command(string name, string[] funcNames, Permission permission, MethodInfo method, string parentId)
        {
            Name = name;
            FuncNames = funcNames;
            Permission = permission;
            Method = method;
            ParentId = parentId;
        }
    }
}
