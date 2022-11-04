using Konata.Core.Message;
using Konata.Core.Message.Model;

namespace RinBot.Common.EventArgs.Messages;

public abstract class MessageEventArgs : AbstractEventArgs
{
    public MessageStruct Message { get; set; }
    public MessageChain Chains => Message.Chain;
    public uint SenderId => Message.Sender.Uin;
    public string SenderName => Message.Sender.Name;
    public uint ReceiverId => Message.Receiver.Uin;
    public string ReceiverName => Message.Receiver.Name;

    public abstract Task<bool> SendMessage(MessageChain chains);
    public Task<bool> SendMessage(MessageBuilder builder)
        => SendMessage(builder.Build());
    public Task<bool> SendMessage(string text)
        => SendMessage(new MessageBuilder(text).Build());

    public Task<bool> Reply(MessageChain chains)
        => SendMessage(new MessageBuilder().Add(ReplyChain.Create(Message)).Add(chains));
    public Task<bool> Reply(MessageBuilder builder)
        => Reply(builder.Build());
    public Task<bool> Reply(string text)
        => Reply(new MessageBuilder(text));

}
