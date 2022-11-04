using Konata.Core.Common;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;

namespace RinBot.Common.EventArgs.Messages;
public class PrivateMessageEventArgs : MessageEventArgs
{
    public BotFriend Friend => Bot.GetFriendList().Result.Where(x => x.Uin == SenderId).First();

    public override Task<bool> SendMessage(MessageChain chains)
        => Bot.SendFriendMessage(SenderId, chains);
}
