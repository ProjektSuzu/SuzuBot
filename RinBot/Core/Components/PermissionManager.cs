using Konata.Core;
using Konata.Core.Interfaces.Api;

namespace RinBot.Core.Components
{
    internal class PermissionManager
    {
        #region 单例模式
        private static PermissionManager instance;
        private PermissionManager() { }
        public static PermissionManager Instance
        {
            get
            {
                if (instance == null) instance = new PermissionManager();
                return instance;
            }
        }
        #endregion

        private uint[] admins = {
            1785416538,
            1156933758,
            1941232341,
            1953909634
        };

        public Permission GetPermission(Bot bot, uint groupUin, uint targetUin)
        {
            if (admins.Contains(targetUin)) return Permission.Admin;
            if (bot.GetGroupMemberInfo(groupUin, targetUin).Result.Role >= Konata.Core.Common.RoleType.Admin) return Permission.Operator;
            return Permission.User;
        }
    }

    internal enum Permission
    {
        User,
        Operator,
        Admin
    }
}
