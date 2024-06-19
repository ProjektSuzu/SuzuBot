using Microsoft.Extensions.DependencyInjection;

namespace SuzuBot.Commands.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal class ModuleAttribute : Attribute
{
    public string Name { get; }
    public ServiceLifetime Lifetime { get; init; }

    public ModuleAttribute(string name)
    {
        Name = name;
    }
}
