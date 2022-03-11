using static ProjektRin.System.PermissionManager;

namespace ProjektRin.Attributes.Command
{
    public class GroupPokeCommand : Command
    {
        public GroupPokeCommand(string name, Permission permission = Permission.User) : base(name, permission)
        {
        }

        public GroupPokeCommand(string name, string pattern, Permission permission = Permission.User) : base(name, pattern, permission)
        {
        }

        public GroupPokeCommand(string name, string[] pattern, Permission permission = Permission.User) : base(name, pattern, permission)
        {
        }
    }
}
