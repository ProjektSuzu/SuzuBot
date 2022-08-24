using Konata.Core.Events.Model;
using RinBot.Core.KonataCore.Events;

namespace RinBot.Core.Components.Managers
{
    internal class EventManager
    {
        #region Singleton
        public static EventManager Instance = new Lazy<EventManager>(() => new EventManager()).Value;
        private EventManager()
        {

        }
        #endregion

        public event EventHandler<MessageEventArgs> OnMessageEvent;
        public event EventHandler<PokeEventArgs> OnPokeEvent;

        public void OnInit()
        {
            // Register EventHandlers
            GlobalScope.KonataBot.Bot.OnGroupMessage += OnKonataGroupMessageEvent;
            GlobalScope.KonataBot.Bot.OnFriendMessage += OnKonataFriendMessageEvent;
            GlobalScope.KonataBot.Bot.OnGroupPoke += OnKonataGroupPokeEvent;
            GlobalScope.KonataBot.Bot.OnFriendPoke += OnKonataFriendPokeEvent;

            OnMessageEvent += (s, e) =>
            {
                GlobalScope.CommandManager.OnBotCommand(e);
            };

            OnPokeEvent += (s, e) =>
            {
                GlobalScope.CommandManager.OnBotCommand(e);
            };
        }

        public void OnKonataGroupMessageEvent(Konata.Core.Bot sender, Konata.Core.Events.Model.GroupMessageEvent groupMessageEvent)
        {
            if (groupMessageEvent.MemberUin == sender.Uin) return;
            if (GlobalScope.PermissionManager.IsGroupInBlackList(groupMessageEvent.GroupUin).Result) return;
            if (GlobalScope.PermissionManager.GetUserInfo(groupMessageEvent.MemberUin).Level == UserPermission.Banned) return;

            var msgEvent = GlobalScope.KonataAdapter.WarpMessageEvent(groupMessageEvent);
            OnMessageEvent.Invoke(sender, msgEvent);
        }
        public void OnKonataFriendMessageEvent(Konata.Core.Bot sender, Konata.Core.Events.Model.FriendMessageEvent friendMessageEvent)
        {
            if (friendMessageEvent.FriendUin == sender.Uin) return;
            if (GlobalScope.PermissionManager.GetUserInfo(friendMessageEvent.FriendUin).Level == UserPermission.Banned) return;

            var msgEvent = GlobalScope.KonataAdapter.WarpMessageEvent(friendMessageEvent);
            OnMessageEvent.Invoke(sender, msgEvent);
        }
        public void OnKonataGroupPokeEvent(Konata.Core.Bot sender, Konata.Core.Events.Model.GroupPokeEvent groupPokeEvent)
        {
            if (groupPokeEvent.OperatorUin == sender.Uin) return;
            if (GlobalScope.PermissionManager.GetUserInfo(groupPokeEvent.OperatorUin).Level == UserPermission.Banned) return;

            var pokeEvent = GlobalScope.KonataAdapter.WarpPokeEvent(groupPokeEvent);
            OnPokeEvent.Invoke(sender, pokeEvent);
        }
        public void OnKonataFriendPokeEvent(Konata.Core.Bot sender, Konata.Core.Events.Model.FriendPokeEvent friendPokeEvent)
        {
            if (friendPokeEvent.FriendUin == sender.Uin) return;
            if (GlobalScope.PermissionManager.GetUserInfo(friendPokeEvent.FriendUin).Level == UserPermission.Banned) return;

            var pokeEvent = GlobalScope.KonataAdapter.WarpPokeEvent(friendPokeEvent);
            OnPokeEvent.Invoke(sender, pokeEvent);
        }

    }
}
