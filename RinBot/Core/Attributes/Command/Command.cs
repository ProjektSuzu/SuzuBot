using ProjektRin.Core.Components;

namespace ProjektRin.Core.Attributes.Command
{
    internal class Command : Attribute
    {
        public readonly string Name;
        public readonly Permission Permission;

        public Command(string name, Permission permission)
        {
            Name = name;
            Permission = permission;
        }
    }
}
