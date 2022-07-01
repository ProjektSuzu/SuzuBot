using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using RinBot.Core.Component.Message;
using RinBot.Core.Component.Message.Model;

namespace RinBot.Core.Component.Event
{
    public enum EventSourceType
    {
        QQ,
        Telegram,
    }

    public enum EventSubjectType
    {
        Bot,
        DirectMessage,
        Group
    }

    internal class RinEvent
    {
        public object OriginalSender { get; private set; }
        public object OriginalEvent { get; private set; }
        public string SenderId { get; private set; }
        public string SubjectId { get; private set; }
        public string RawContent { get; private set; }
        public EventSourceType EventSourceType { get; private set; }
        public EventSubjectType EventSubjectType { get; private set; }

        internal RinEvent(object originalSender, object originalEvent, string rawContent, EventSourceType eventSourceType, EventSubjectType eventSubjectType, string senderID = "", string subjectID = "")
        {
            OriginalSender = originalSender;
            OriginalEvent = originalEvent;
            RawContent = rawContent;
            EventSourceType = eventSourceType;
            EventSubjectType = eventSubjectType;
            SenderId = senderID;
            SubjectId = subjectID;
        }

        public Task<bool> Reply(RinMessageChain chains)
        {
            switch (EventSourceType)
            {
                case EventSourceType.QQ:
                    return KonataMessageReply(chains);

                default:
                    throw new Exception("Unsupported event source type.");
            }
        }

        private Task<bool> KonataMessageReply(RinMessageChain chains)
        {
            if (EventSubjectType == EventSubjectType.Bot)
                throw new Exception("Unsupported subject type.");

            var bot = (Bot)OriginalSender;

            var messageBuilder = new Konata.Core.Message.MessageBuilder();
            foreach (var chain in chains)
            {
                switch (chain.Type)
                {
                    case BaseChain.ChainType.Text:
                        messageBuilder.Text((chain as TextChain).Content);
                        break;

                    case BaseChain.ChainType.Image:
                        messageBuilder.Image((chain as ImageChain).Bytes);
                        break;

                    case BaseChain.ChainType.At:
                        if (EventSubjectType == EventSubjectType.Group)
                        {
                            messageBuilder.At(uint.Parse((chain as AtChain).TargetID));
                        }
                        break;

                    case BaseChain.ChainType.Reply:
                        if (EventSubjectType == EventSubjectType.Group)
                        {
                            messageBuilder.Add(Konata.Core.Message.Model.ReplyChain.Create(((chain as ReplyChain).TargetMessageEvent as GroupMessageEvent).Message));
                        }
                        else
                        {
                            messageBuilder.Add(Konata.Core.Message.Model.ReplyChain.Create(((chain as ReplyChain).TargetMessageEvent as FriendMessageEvent).Message));
                        }
                        break;


                    default:
                        throw new Exception("Unsupported chain type.");
                }
            }

            if (EventSubjectType == EventSubjectType.Group)
            {
                if (OriginalEvent is GroupMessageEvent)
                {
                    var groupMessageEvent = (GroupMessageEvent)OriginalEvent;
                    return bot.SendGroupMessage(groupMessageEvent.GroupUin, messageBuilder);
                }
            }
            else
            {
                if (OriginalEvent is FriendMessageEvent)
                {
                    var friendMessageEvent = (FriendMessageEvent)OriginalEvent;
                    return bot.SendFriendMessage(friendMessageEvent.FriendUin, messageBuilder);
                }
            }
            //should throw here
            return Task.FromResult(true);
        }
    }
}
