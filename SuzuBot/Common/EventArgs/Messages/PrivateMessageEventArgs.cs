using Konata.Core.Common;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;

namespace SuzuBot.Common.EventArgs.Messages;
public class PrivateMessageEventArgs : MessageEventArgs
{
    public Lazy<BotFriend> Friend => new(() => Bot.GetFriendList().Result.Where(x => x.Uin == SenderId).First());

    public override string SenderName => Friend.Value.Name;
    public override string ReceiverName => Bot.Name;

    public override Task<bool> SendMessage(MessageChain chains)
        => Bot.SendFriendMessage(SenderId, chains);
}
