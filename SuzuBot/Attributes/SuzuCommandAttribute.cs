namespace SuzuBot.Attributes;
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal class SuzuCommandAttribute : Attribute
{
    public string Name { get; set; }
    public string Pattern { get; set; }
    public byte Priority { get; set; } = 127;
    public bool UsePrefix { get; set; } = false;

    public SuzuCommandAttribute(string name, string pattern)
    {
        Name = name;
        Pattern = pattern;
    }
}
