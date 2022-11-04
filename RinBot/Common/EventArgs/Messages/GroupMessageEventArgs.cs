using Konata.Core.Common;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;

namespace RinBot.Common.EventArgs.Messages;
public class GroupMessageEventArgs : MessageEventArgs
{
    public BotGroup Group => Bot.GetGroupList().Result.Where(x => x.Uin == ReceiverId).First();
    public BotMember Member => Bot.GetGroupMemberList(ReceiverId).Result.Where(x => x.Uin == SenderId).First();

    public override Task<bool> SendMessage(MessageChain chains)
        => Bot.SendGroupMessage(ReceiverId, chains);
}
