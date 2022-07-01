using Konata.Core;
using Konata.Core.Events.Model;

namespace RinBot.Core.Component.Event
{
    internal static class EventWrapper
    {
        public static RinEvent ToRinEvent(this GroupMessageEvent messageEvent, Bot bot)
        {
            return new RinEvent(
                bot,
                messageEvent,
                messageEvent.Message.Chain.ToString().Trim(),
                EventSourceType.QQ,
                EventSubjectType.Group,
                messageEvent.MemberUin.ToString(),
                messageEvent.GroupUin.ToString()
                );
        }

        public static RinEvent ToRinEvent(this FriendMessageEvent messageEvent, Bot bot)
        {
            return new RinEvent(
                bot,
                messageEvent,
                messageEvent.Message.Chain.ToString().Trim(),
                EventSourceType.QQ,
                EventSubjectType.DirectMessage,
                messageEvent.FriendUin.ToString(),
                messageEvent.FriendUin.ToString()
                );
        }

        public static RinEvent ToRinEvent(this GroupPokeEvent pokeEvent, Bot bot)
        {
            return new RinEvent(
                bot,
                pokeEvent,
                $"{pokeEvent.OperatorUin} {pokeEvent.ActionPrefix} {pokeEvent.MemberUin} {pokeEvent.ActionSuffix}",
                EventSourceType.QQ,
                EventSubjectType.DirectMessage,
                pokeEvent.OperatorUin.ToString(),
                pokeEvent.GroupUin.ToString()
                );
        }

        public static RinEvent ToRinEvent(this FriendPokeEvent pokeEvent, Bot bot)
        {
            return new RinEvent(
                bot,
                pokeEvent,
                $"{pokeEvent.FriendUin} {pokeEvent.ActionPrefix} {pokeEvent.SelfUin} {pokeEvent.ActionSuffix}",
                EventSourceType.QQ,
                EventSubjectType.DirectMessage,
                pokeEvent.FriendUin.ToString(),
                pokeEvent.FriendUin.ToString()
                );
        }
    }
}
