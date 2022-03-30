using Konata.Core.Common;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;

namespace ProjektRin.Components
{
    public class PermissionManager
    {
        private static readonly PermissionManager _instance = new PermissionManager();

        private PermissionManager() { }

        public static PermissionManager Instance => _instance;

        private readonly List<uint> admins = new()
        {
            1785416538,
            1156933758,
        };
        public enum Permission
        {
            User,
            Operator,
            Root
        }

        public bool IsOperator(uint groupUin, uint memberUin)
        {
            return BotManager.Instance.Bot.GetGroupMemberInfo(groupUin, memberUin)?.Result.Role >= RoleType.Admin;
        }

        public bool IsOperator(GroupMessageEvent groupMessageEvent)
        {
            return IsOperator(groupMessageEvent.GroupUin, groupMessageEvent.MemberUin);
        }

        public bool IsAdmin(uint uin)
        {
            return admins.Contains(uin);
        }

        //或许以后可以像Luckperms那样搞个权限树 但是我选择摸
    }
}
