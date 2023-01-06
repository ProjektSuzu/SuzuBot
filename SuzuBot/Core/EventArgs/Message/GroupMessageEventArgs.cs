using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using SuzuBot.Core.Contacts;

namespace SuzuBot.Core.EventArgs.Message;
public class GroupMessageEventArgs : MessageEventArgs
{
    public Lazy<Group> Group => new Lazy<Group>(Contacts.Group.GetSuzuGroup(Bot, Subject.Id));
    public Lazy<Member> Member => new Lazy<Member>(Contacts.Member.GetSuzuMember(Bot, Subject.Id, Sender.Id));

    public override Task<bool> Reply(MessageChain chain)
    {
        var builder = new MessageBuilder(chain)
        {
            ReplyChain.Create(Message)
        };
        return Bot.SendGroupMessage(Subject.Id, builder);
    }
}
