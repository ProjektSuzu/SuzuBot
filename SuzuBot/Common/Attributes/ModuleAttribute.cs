namespace SuzuBot.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
internal class ModuleAttribute : Attribute
{
    public string Name { get; set; }
    public bool IsCritical { get; set; }

    public ModuleAttribute(string name)
    {
        Name = name;
    }
}
