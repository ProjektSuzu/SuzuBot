namespace SuzuBot.Core.Attributes;

public enum AuthGroup
{
    Root,
    Admin,
    User
}

public enum SourceType
{
    Friend = 1,
    Group,
    All = Group | Friend
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CommandAttribute : Attribute
{
    public string Name { get; set; }
    public string[] Patterns { get; set; }
    public AuthGroup AuthGroup { get; set; } = AuthGroup.User;
    public byte Priority { get; set; } = 127;
    public SourceType SourceType { get; set; } = SourceType.All;
    public bool IgnorePrefix { get; set; } = false;
    public bool WarnOnAuthFail { get; set; } = false;

    public CommandAttribute(string name, params string[] patterns)
    {
        Name = name;
        Patterns = patterns;
    }
}
