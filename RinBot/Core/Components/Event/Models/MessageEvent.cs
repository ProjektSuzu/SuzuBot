using RinBot.Core.Components.Messages;

namespace RinBot.Core.Components.Event.Models
{
    internal class MessageEvent : BaseEvent
    {
        public string SenderId { get; private set; }
        public string SenderName { get; private set; }
        public string SubjectId { get; private set; }
        public RinMessageChain Chains { get; private set; }
        public string RawContent { get; private set; }

        public MessageEvent(object source,
                            object sender,
                            EventSourceType sourceType,
                            EventSenderType senderType,
                            string senderId,
                            string senderName,
                            string subjectId,
                            RinMessageChain chains,
                            string rawContent)
            : base(source, sender, sourceType, senderType)
        {
            SenderId = senderId;
            SenderName = senderName;
            SubjectId = subjectId;
            Chains = chains;
            RawContent = rawContent;
        }
    }
}
