namespace SuzuBot.Commands.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal class ShortcutAttribute : Attribute
{
    public string Pattern { get; }
    public string FormatString { get; }
    public Prefix Prefix { get; init; } = Prefix.None;

    public ShortcutAttribute(string pattern, string formatString)
    {
        Pattern = pattern;
        FormatString = formatString;
    }
}
