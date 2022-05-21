using ProjektRin.Core.Components;

namespace ProjektRin.Core.Attributes.Command.Modules
{
    internal class GroupPokeCommand : Command
    {
        public GroupPokeCommand(string name, Permission permission) : base(name, permission)
        {
        }
    }
}
