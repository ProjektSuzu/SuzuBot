namespace RinBot.Core.Components.Event
{
    internal abstract class BaseEvent
    {
        public enum EventSourceType : short
        {
            QQ,
            KOOK,
        }

        public enum EventSenderType : short
        {
            Bot,
            Direct,
            Group
        }

        public object Source { get; protected set; }
        public object Sender { get; protected set; }

        public EventSourceType SourceType { get; protected set; }
        public EventSenderType SenderType { get; protected set; }

        protected BaseEvent(object source, object sender, EventSourceType sourceType, EventSenderType senderType)
        {
            Source = source;
            Sender = sender;
            SourceType = sourceType;
            SenderType = senderType;
        }
    }
}
