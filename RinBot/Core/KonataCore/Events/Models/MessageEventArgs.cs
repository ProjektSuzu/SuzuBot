using Konata.Core.Message;
using Konata.Core.Message.Model;
using RinBot.Core.Components;
using RinBot.Core.KonataCore.Contacts;

namespace RinBot.Core.KonataCore.Events
{
    internal class MessageEventArgs : RinEventArgs
    {
        public BotContact Subject { get; internal set; }
        public BotContact Sender { get; internal set; }
        // 以后想写多 IM 平台兼容的时候记得把这里改成自己实现的 MessageStruct
        public MessageStruct Message { get; internal set; }

        public SubjectType SubjectType
        {
            get
            {
                switch (Message.Type)
                {
                    case MessageStruct.SourceType.Friend:
                        return SubjectType.Friend;
                    case MessageStruct.SourceType.Group:
                        return SubjectType.Group;
                    case MessageStruct.SourceType.Stranger:
                        return SubjectType.Temp;
                    default:
                        throw new InvalidCastException();
                }
            }
        }

        public Task<bool> Reply(MessageChain chains)
        {
            chains = new MessageBuilder(ReplyChain.Create(Message))
            {
                chains
            }.Build();
            return Subject.SendMessage(chains);
        }

        public Task<bool> Reply(MessageBuilder builder)
            => Reply(builder.Build());
        public Task<bool> Reply(string text)
            => Reply(new MessageBuilder(text).Build());
    }
}
