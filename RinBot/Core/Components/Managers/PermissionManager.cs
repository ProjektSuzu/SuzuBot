using RinBot.Core.Components.Databases.Tables;
using RinBot.Core.KonataCore.Contacts;
using RinBot.Core.KonataCore.Contacts.Models;

namespace RinBot.Core.Components.Managers
{
    internal class PermissionManager
    {
        #region Singleton
        public static PermissionManager Instance = new Lazy<PermissionManager>(() => new PermissionManager()).Value;
        private PermissionManager()
        {

        }
        #endregion

        public UserPermission GetUserLevel(BotContact user)
        {
            if (user is Group)
            {
                throw new NotSupportedException();
            }

            var userInfo = GlobalScope.DatabaseManager.DBConnection
                .Table<QQUserInfo>()
                .Where(x => x.Uin == user.Uin)
                .FirstOrDefaultAsync().Result;

            if (user is Member member)
            {
                switch (userInfo.Level)
                {
                    case UserPermission.Root:
                        return UserPermission.Root;
                    case UserPermission.Admin:
                        return UserPermission.Admin;
                    case UserPermission.User:
                        if (member.IsAdmin)
                            return UserPermission.GroupAdmin;
                        else
                            return UserPermission.User;
                    case UserPermission.Banned:
                        return UserPermission.Banned;
                    default:
                        throw new NotSupportedException();
                }
            }
            else if (user is Friend friend)
            {
                return userInfo.Level;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public QQUserInfo GetUserInfo(uint uin)
        {
            var info = GlobalScope.DatabaseManager.DBConnection
                .Table<QQUserInfo>()
                .Where(x => x.Uin == uin)
                .FirstOrDefaultAsync().Result;
            if (info == null)
            {
                info = new()
                {
                    Uin = uin,
                    Level = UserPermission.User,
                    Coin = 0,
                    Exp = 0,
                    Favor = 0,
                };
                _ = GlobalScope.DatabaseManager.DBConnection
                    .InsertAsync(info);
                return info;
            }
            else
                return info;
        }
        public bool UpdateUserInfo(QQUserInfo info)
            => GlobalScope.DatabaseManager.DBConnection
            .UpdateAsync(info).Result > 0;
        public QQGroupInfo GetGroupInfo(uint uin)
        {
            var info = GlobalScope.DatabaseManager.DBConnection
                .Table<QQGroupInfo>()
                .Where(x => x.Uin == uin)
                .FirstOrDefaultAsync().Result;
            if (info == null)
            {
                var group = GlobalScope.KonataAdapter.GetGroup(uin).Result;
                info = new()
                {
                    Uin = uin,
                    InviterUin = group.Owner.Uin,
                    ModuleIds = new()
                };
                _ = GlobalScope.DatabaseManager.DBConnection
                    .InsertAsync(info);
                return info;
            }
            else
                return info;
        }
        public bool UpdateGroupInfo(QQGroupInfo info)
            => GlobalScope.DatabaseManager.DBConnection
            .UpdateAsync(info).Result > 0;
        public async Task<bool> IsGroupInBlackList(uint groupUin)
        {
            return await GlobalScope.DatabaseManager.DBConnection
                .Table<QQGroupBlackList>()
                .Where(x => x.Uin == groupUin)
                .FirstOrDefaultAsync() != null;
        }
        public async Task<bool> IsGroupInWhiteList(uint groupUin)
        {
            return await GlobalScope.DatabaseManager.DBConnection
                .Table<QQGroupWhiteList>()
                .Where(x => x.Uin == groupUin)
                .FirstOrDefaultAsync() != null;
        }
        public void OnInit()
        {

        }
    }
}
