namespace RinBot.Core.Components.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class CommandAttribute : Attribute
    {
        public string Name { get; protected set; }
        public string[] FuncTokens { get; protected set; }
        public UserPermission Permission { get; protected set; }

        public CommandAttribute(string name, string funcName, UserPermission permission = UserPermission.User)
        {
            Name = name;
            FuncTokens = new[] { funcName };
            Permission = permission;
        }

        public CommandAttribute(string name, string[] funcNames, UserPermission permission = UserPermission.User)
        {
            Name = name;
            FuncTokens = funcNames;
            Permission = permission;
        }
    }
}
