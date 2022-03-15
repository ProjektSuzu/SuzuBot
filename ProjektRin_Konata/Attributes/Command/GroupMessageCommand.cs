using static ProjektRin.Components.PermissionManager;

namespace ProjektRin.Attributes.Command
{
    public class GroupMessageCommand : Command
    {
        public GroupMessageCommand(string name, Permission permission = Permission.User) : base(name, permission)
        {
        }

        public GroupMessageCommand(string name, string pattern, Permission permission = Permission.User) : base(name, pattern, permission)
        {
        }

        public GroupMessageCommand(string name, string[] pattern, Permission permission = Permission.User) : base(name, pattern, permission)
        {
        }
    }
}
