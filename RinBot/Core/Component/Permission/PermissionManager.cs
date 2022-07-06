using Konata.Core.Interfaces.Api;
using RinBot.Core.Component.Database;

namespace RinBot.Core.Component.Permission
{
    public enum UserRole
    {
        Banned,
        User,
        Operator,
        Admin
    }

    internal class PermissionManager
    {
        #region Singleton
        private static PermissionManager instance;
        public static PermissionManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new();
                return instance;
            }
        }
        private PermissionManager() 
        {
            database.dbConnection.CreateTable<QQUserInfo>(); 
            database.dbConnection.CreateTable<QQGroupInfo>();
        }
        #endregion
        private readonly RinDatabase database = RinDatabase.Instance;

        public UserRole GetQQUserRole(uint userId)
        {
            QQUserInfo? info = database.dbConnection
                .Table<QQUserInfo>()
                .FirstOrDefault(x => x.UserId == userId);

            if (info == null)
            {
                info = new()
                {
                    UserId = userId,
                    UserRole = UserRole.User,
                    Exp = 0,
                    Memory = 0,
                };
                database.dbConnection.Insert(info);
                return UserRole.User;
            }
            return info.UserRole;
        }

        public UserRole GetQQUserRoleInGroup(uint userId, uint groupId)
        {
            var info = KonataCore.KonataBot.Instance.Bot.GetGroupMemberInfo(userId, groupId).Result;
            if (info.Role >= Konata.Core.Common.RoleType.Admin)
                return UserRole.Operator;
            return UserRole.User;
        }

        public QQGroupInfo GetQQGroupInfo(uint groupId)
        {
            QQGroupInfo? info = database.dbConnection
                .Table<QQGroupInfo>()
                .FirstOrDefault(x => x.GroupId == groupId);

            if (info == null)
            {
                info = new()
                {
                    GroupId = groupId,
                    InviterId = 114514,
                    DisableModuleIds = new(),
                    WhiteListed = false,
                    BlackListed = false,
                };
                database.dbConnection.Insert(info);
                return info;
            }
            return info;
        }

        public QQUserInfo GetQQUserInfo(uint userId)
        {
            QQUserInfo? info = database.dbConnection
                .Table<QQUserInfo>()
                .FirstOrDefault(x => x.UserId == userId);

            if (info == null)
            {
                info = new()
                {
                    UserId = userId,
                    UserRole = UserRole.User,
                    Exp = 0,
                    Memory = 0,
                };
                database.dbConnection.Insert(info);
                return info;
            }
            return info;
        }

        public bool IsQQGroupWhiteListed(uint groupId)
            => GetQQGroupInfo(groupId).WhiteListed;

        public bool IsQQGroupBlackListed(uint groupId)
            => GetQQGroupInfo(groupId).BlackListed;

    }
}
