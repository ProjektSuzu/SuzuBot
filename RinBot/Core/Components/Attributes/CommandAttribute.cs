namespace RinBot.Core.Components.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class CommandAttribute : Attribute
    {
        public string Name { get; protected set; }
        public string[] FuncNames { get; protected set; }
        public Permission Permission { get; protected set; }

        public CommandAttribute(string name, string funcName, Permission permission = Permission.User)
        {
            Name = name;
            FuncNames = new[] { funcName };
            Permission = permission;
        }

        public CommandAttribute(string name, string[] funcNames, Permission permission = Permission.User)
        {
            Name = name;
            FuncNames = funcNames;
            Permission = permission;
        }
    }
}
