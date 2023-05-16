namespace SuzuBot.Attributes;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal class SuzuModuleAttribute : Attribute
{
    public string Name { get; set; }
    public bool IsCore { get; set; } = false;

    public SuzuModuleAttribute(string name)
    {
        Name = name;
    }
}
