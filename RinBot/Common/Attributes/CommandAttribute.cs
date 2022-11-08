namespace RinBot.Common.Attributes;

[Flags]
public enum MatchType
{
    Equal,
    StartsWith,
    Contains,
    EndsWith,
    Regex,
    NoPrefix
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CommandAttribute : Attribute
{
    public string Name { get; set; }
    public string[] Commands { get; set; }
    public byte Priority { get; set; } = 127;
    public string AuthGroup { get; set; } = "user";
    public bool AuthFailWarning { get; set; } = false;
    public MatchType MatchType { get; set; } = MatchType.Equal;
    public SourceType SourceType { get; set; } = SourceType.All;
    public HandlerType HandlerType { get; set; } = HandlerType.Block;

    public CommandAttribute(string name, string command)
    {
        Name = name;
        Commands = new[] { command };
    }

    public CommandAttribute(string name, params string[] commands)
    {
        Name = name;
        Commands = commands;
    }
}
