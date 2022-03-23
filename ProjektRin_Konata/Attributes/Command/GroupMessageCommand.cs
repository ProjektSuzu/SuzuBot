using static ProjektRin.Components.PermissionManager;

namespace ProjektRin.Attributes.Command
{
    public class GroupMessageCommand : Command
    {
        public GroupMessageCommand(string name, Permission permission = Permission.User, bool isRaw = false) : base(name, permission, isRaw: isRaw)
        {
        }

        public GroupMessageCommand(string name, string pattern, Permission permission = Permission.User, bool isRaw = false) : base(name, pattern, permission, isRaw: isRaw)
        {
        }

        public GroupMessageCommand(string name, string[] pattern, Permission permission = Permission.User, bool isRaw = false) : base(name, pattern, permission, isRaw: isRaw)
        {
        }
    }
}
