using Konata.Core.Common;
using Konata.Core.Interfaces.Api;
using SQLite;
using SuzuBot.Core.Databases.Tables;

namespace SuzuBot.Core.Auth;

internal class AuthManager : BaseManager
{
    private SQLiteAsyncConnection _connection;

    public AuthManager(Context context) : base(context)
    {
        _connection = Context.DataBaseManager.Connection;
    }

    public byte GetAuthGroupPriority(string authGroup)
    {
        return _connection
            .Table<SuzuAuthGroup>()
            .Where(x => x.AuthGroup == authGroup)
            .FirstOrDefaultAsync()
            .Result.Priority;
    }
    public byte GetUserAuth(uint uin)
    {
        SuzuUserInfo userInfo = _connection
            .Table<SuzuUserInfo>()
            .Where(x => x.Uin == uin)
            .FirstOrDefaultAsync().Result;

        if (userInfo == null)
        {
            userInfo = new()
            {
                Uin = uin
            };
            _connection.InsertAsync(userInfo);
            return GetAuthGroupPriority(userInfo.AuthGroup);
        }
        else return GetAuthGroupPriority(userInfo.AuthGroup);
    }
    public byte GetMemberAuth(uint memberUin, uint groupUin)
    {
        BotMember? member = Context.Bot.GetGroupMemberList(groupUin).Result
            .Where(x => x.Uin == memberUin).FirstOrDefault();
        if (member is null) return 0;
        else
        {
            var priority = member.Role >= RoleType.Admin
                ? GetAuthGroupPriority("operator")
                : GetAuthGroupPriority("user");
            return Math.Max(GetUserAuth(memberUin), priority);
        }
    }
}
