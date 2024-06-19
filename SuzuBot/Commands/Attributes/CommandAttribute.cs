namespace SuzuBot.Commands.Attributes;

[AttributeUsage(AttributeTargets.Method)]
internal class CommandAttribute : Attribute
{
    public string Name { get; }
    public string[] Aliases { get; }

    public CommandAttribute(string name, params string[] aliases)
    {
        Name = name;
        Aliases = aliases;
    }
}
