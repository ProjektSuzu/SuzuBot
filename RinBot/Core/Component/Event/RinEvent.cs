using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using RinBot.Core.Component.Message;
using RinBot.Core.Component.Message.Model;

namespace RinBot.Core.Component.Event
{
    public enum EventSourceType
    {
        QQ = 1,
        Telegram = 2,
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

        public string GetSenderName()
        {
            switch (EventSourceType)
            {
                case EventSourceType.QQ:
                    {
                        if (EventSubjectType == EventSubjectType.Group)
                        {
                            return (OriginalSender as Bot).GetGroupMemberInfo((OriginalEvent as GroupMessageEvent).GroupUin, (OriginalEvent as GroupMessageEvent).MemberUin).Result.NickName;
                        }
                        else
                        {
                            return (OriginalSender as Bot).GetFriendList().Result.First(x => x.Uin == (OriginalEvent as FriendMessageEvent).FriendUin).Name;
                        }
                    }

                default: return "unknown";
            }
        }

        public string GetSubjectName()
        {
            switch (EventSourceType)
            {
                case EventSourceType.QQ:
                    {
                        if (EventSubjectType == EventSubjectType.Group)
                        {
                            return (OriginalSender as Bot).GetGroupList().Result.First(x => x.Uin == (OriginalEvent as GroupMessageEvent).GroupUin).Name;
                        }
                        else
                        {
                            return GetSenderName();
                        }
                    }

                default: return "unknown";
            }
        }

        public Task<bool> Reply(string text)
        {
            return Reply(new RinMessageChain(TextChain.Create(text)));
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

        internal Task<bool> KonataMessageReply(Konata.Core.Message.MessageChain chains)
        {
            if (EventSubjectType == EventSubjectType.Bot)
                throw new Exception("Unsupported subject type.");

            var bot = (Bot)OriginalSender;

            if (EventSubjectType == EventSubjectType.Group)
            {
                if (OriginalEvent is GroupMessageEvent)
                {
                    var groupMessageEvent = (GroupMessageEvent)OriginalEvent;
                    return bot.SendGroupMessage(groupMessageEvent.GroupUin, chains);
                }
            }
            else
            {
                if (OriginalEvent is FriendMessageEvent)
                {
                    var friendMessageEvent = (FriendMessageEvent)OriginalEvent;
                    return bot.SendFriendMessage(friendMessageEvent.FriendUin, chains);
                }
            }
            //should throw here
            return Task.FromResult(true);
        }

        internal Task<bool> KonataMessageReply(RinMessageChain chains)
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
