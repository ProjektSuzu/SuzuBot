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

        public void OnInit()
        {
            // Register EventHandlers
            GlobalScope.KonataBot.Bot.OnGroupMessage += OnKonataGroupMessageEvent;
            GlobalScope.KonataBot.Bot.OnFriendMessage += OnKonataFriendMessageEvent;

            OnMessageEvent += (s, e) =>
            {
                GlobalScope.CommandManager.OnBotCommand(e);
            };
        }

        public void OnKonataGroupMessageEvent(Konata.Core.Bot sender, Konata.Core.Events.Model.GroupMessageEvent groupMessageEvent)
        {
            if (groupMessageEvent.MemberUin == sender.Uin) return;
            if (GlobalScope.PermissionManager.IsGroupInBlackList(groupMessageEvent.GroupUin).Result) return;
            if (GlobalScope.PermissionManager.GetQQUserInfo(groupMessageEvent.MemberUin).Level == UserPermission.Banned) return;

            var msgEvent = GlobalScope.KonataAdapter.WarpMessageEvent(groupMessageEvent);
            OnMessageEvent.Invoke(sender, msgEvent);
        }

        public void OnKonataFriendMessageEvent(Konata.Core.Bot sender, Konata.Core.Events.Model.FriendMessageEvent friendMessageEvent)
        {
            if (friendMessageEvent.FriendUin == sender.Uin) return;
            if (GlobalScope.PermissionManager.GetQQUserInfo(friendMessageEvent.FriendUin).Level == UserPermission.Banned) return;

            var msgEvent = GlobalScope.KonataAdapter.WarpMessageEvent(friendMessageEvent);
            OnMessageEvent.Invoke(sender, msgEvent);
        }
    }
}
