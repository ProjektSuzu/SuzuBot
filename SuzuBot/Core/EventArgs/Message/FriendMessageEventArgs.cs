using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using SuzuBot.Core.Contacts;

namespace SuzuBot.Core.EventArgs.Message;
public class FriendMessageEventArgs : MessageEventArgs
{
    public Lazy<Friend> Friend => new Lazy<Friend>(Contacts.Friend.GetSuzuFriend(Bot, Sender.Id));

    public override Task<bool> SendMessage(MessageChain chain) => Bot.SendFriendMessage(Sender.Id, chain);
}
