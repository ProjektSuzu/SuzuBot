using SuzuBot.Core.Attributes;

namespace SuzuBot.Core.Manager;

public class AuthManager : BaseManager
{
    public AuthManager(Context context) : base(context)
    {
    }

    public AuthGroup GetUserAuthGroup(uint uin)
    {
        return Context.DatabaseManager.GetUserInfo(uin).AuthGroup;
    }
}