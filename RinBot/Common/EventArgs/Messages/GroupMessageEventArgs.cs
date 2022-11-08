using Konata.Core.Common;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;

namespace RinBot.Common.EventArgs.Messages;
public class GroupMessageEventArgs : MessageEventArgs
{
    public Lazy<BotGroup> Group => new(() => Bot.GetGroupList().Result.Where(x => x.Uin == ReceiverId).First());
    public Lazy<BotMember> Member => new(() => Bot.GetGroupMemberInfo(ReceiverId, SenderId).Result);

    public override Task<bool> SendMessage(MessageChain chains)
        => Bot.SendGroupMessage(ReceiverId, chains);
}
