namespace SuzuBot.Core.Attributes;

public enum AuthGroup
{
    Root,
    Admin,
    User
}

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string Name { get; set; }
    public string[] Patterns { get; set; }
    public AuthGroup AuthGroup { get; set; } = AuthGroup.User;
    public byte Priority { get; set; } = 127;
    public bool IgnorePrefix { get; set; } = false;
    public bool WarnOnAuthFail { get; set; } = false;

    public CommandAttribute(string name, params string[] patterns)
    {
        Name = name;
        Patterns = patterns;
    }
}
