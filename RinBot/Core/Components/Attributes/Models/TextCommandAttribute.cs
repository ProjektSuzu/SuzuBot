namespace RinBot.Core.Components.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class TextCommandAttribute : CommandHandlerAttribute
    {
        public string[] FuncTokens { get; protected set; }
        public UserPermission Permission { get; protected set; }

        public TextCommandAttribute(string name, string funcName, UserPermission permission = UserPermission.User)
        {
            Name = name;
            FuncTokens = new[] { funcName };
            Permission = permission;
        }

        public TextCommandAttribute(string name, string[] funcNames, UserPermission permission = UserPermission.User)
        {
            Name = name;
            FuncTokens = funcNames;
            Permission = permission;
        }
    }
}
