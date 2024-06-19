namespace SuzuBot.Commands.Attributes;

[AttributeUsage(AttributeTargets.Method)]
internal class RouteRuleAttribute : Attribute
{
    public Permission Permission { get; init; } = Permission.User;
    public Prefix Prefix { get; init; } = Prefix.Prefix | Prefix.Mention;
    public byte Priority { get; init; } = 127;
}

internal enum Permission
{
    Banned = -128,
    Everyone = 0,
    User = 1,
    Admin = 3,
    Owner = 7,
    Disabled = 127
}

[Flags]
internal enum Prefix
{
    None = 0,
    Prefix = 1,
    Mention = 2
}
