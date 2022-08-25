namespace RinBot.Core.Components
{
    public enum UserPermission : short
    {
        Banned,
        User,
        GroupAdmin,
        Admin,
        Root,
    }
    internal enum SubjectType : short
    {
        Friend,
        Group,
        Temp,
    }

    public enum PokeReceiveTarget : short
    {
        Any,
        Bot,
    }
    internal enum ModuleEnableType : short
    {
        NormallyEnabled,
        NormallyDisabled,
        WhiteListOnly,
    }
}
