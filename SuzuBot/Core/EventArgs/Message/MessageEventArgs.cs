using Konata.Core.Message;
using Konata.Core.Message.Model;

namespace SuzuBot.Core.EventArgs.Message;
public abstract class MessageEventArgs : SuzuEventArgs
{
    public required MessageStruct Message { get; set; }
    public (uint Id, string Name) Sender => Message.Sender;
    public (uint Id, string Name) Subject => Message.Receiver;
    public MessageChain Chain => Message.Chain;

    public abstract Task<bool> SendMessage(MessageChain chains);
    public Task<bool> SendMessage(MessageBuilder builder)
        => SendMessage(builder.Build());
    public Task<bool> SendMessage(string text)
        => SendMessage(new MessageBuilder(text).Build());

    public Task<bool> Reply(MessageChain chain) => SendMessage(new MessageBuilder().Add(ReplyChain.Create(Message)).Add(chains));
    public Task<bool> Reply(MessageBuilder builder) => Reply(builder.Build());
    public Task<bool> Reply(string text) => Reply(new MessageBuilder(text).Build());
}
