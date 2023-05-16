using System.ComponentModel.DataAnnotations;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using SuzuBot.Events;
using static Konata.Core.Message.MessageStruct;

namespace SuzuBot.EventArgs;
internal class MessageEventArgs : SuzuEventArgs
{
    public MessageStruct Message { get; init; }
    public MessageChain Chain => Message.Chain;
    public SourceType SourceType => Message.Type;
    public (uint Uin, string Name) Sender => Message.Sender;
    public (uint Uin, string Name) Receiver => Message.Receiver;

    public Task SendMessage(params BaseChain[] chain)
    {
        switch (SourceType)
        {
            case SourceType.Friend:
                return Bot.SendFriendMessage(Sender.Uin, chain);
            case SourceType.Group:
                return Bot.SendGroupMessage(Receiver.Uin, chain);

            default:
                throw new NotImplementedException();
        }
    }

    public Task SendMessage(MessageChain chain)
    {
        return SendMessage(chain.ToArray());
    }

    public Task SendMessage(MessageBuilder builder)
    {
        return SendMessage(builder.Build());
    }

    public Task Reply(params BaseChain[] chain)
    {
        var message = new MessageBuilder()
        {
            ReplyChain.Create(Message),
            chain
        };
        return SendMessage(message);
    }

    public Task Reply(MessageChain chain)
    {
        return Reply(chain.ToArray());
    }

    public Task Reply(MessageBuilder builder)
    {
        builder.Add(ReplyChain.Create(Message));
        return SendMessage(builder);
    }

    public async Task<MessageEventArgs?> Next(TimeSpan timeout, Predicate<MessageEventArgs>? predicate = null, CancellationToken? token = null)
    {
        MessageEventArgs? result = null;
        CancellationTokenSource cts = new();
        Task delay;
        var observer = Observer<SuzuEventArgs>.Create(x =>
        {
            if (x is not MessageEventArgs eventArgs) return Task.CompletedTask;
            else
            {
                if (eventArgs.Sender.Uin != Sender.Uin || eventArgs.Receiver.Uin != Receiver.Uin) return Task.CompletedTask;
                if (predicate is not null && predicate(eventArgs)) return Task.CompletedTask;
                result = eventArgs;
                cts.Cancel();
                return Task.CompletedTask;
            }
        });
        EventBus.Subscribe(observer);
        delay = Task.Delay(timeout, cts.Token);
        if (token is not null)
        {
            _ = Task.Run(() =>
            {
                while (!token.Value.IsCancellationRequested) ;
                cts.Cancel();
            });
        }
        await delay;

        return result;
    }
}
