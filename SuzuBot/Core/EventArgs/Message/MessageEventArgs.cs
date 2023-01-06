using Konata.Core.Message;

namespace SuzuBot.Core.EventArgs.Message;
public abstract class MessageEventArgs : SuzuEventArgs
{
    public required MessageStruct Message { get; set; }
    public (uint Id, string Name) Sender => Message.Sender;
    public (uint Id, string Name) Subject => Message.Receiver;
    public MessageChain Chain => Message.Chain;

    public abstract Task<bool> Reply(MessageChain chain);
    public Task<bool> Reply(MessageBuilder builder) => Reply(builder.Build());
    public Task<bool> Reply(string text) => Reply(new MessageBuilder(text).Build());
}
