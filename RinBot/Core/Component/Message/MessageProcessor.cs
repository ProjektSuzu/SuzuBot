using Konata.Core;
using Konata.Core.Events.Model;
using NLog;
using RinBot.Core.Component.Command;
using RinBot.Core.Component.Event;

namespace RinBot.Core.Component.Message
{
    internal class MessageProcessor
    {
        #region Singleton
        private static MessageProcessor instance;
        public static MessageProcessor Instance
        {
            get
            {
                if (instance == null)
                    instance = new();
                return instance;
            }
        }
        private MessageProcessor() { }
        #endregion

        Logger Logger = LogManager.GetLogger("MSG");

        public void OnKonataGroupMessage(object sender, GroupMessageEvent e)
        {
            Logger.Info($"QQ {e.GroupName}(G{e.GroupUin}) {e.MemberCard}(U{e.MemberUin})\n{e.Message.Chain}");
            if (e.MemberUin == (sender as Bot).Uin) return;
            var rinMessageEvent = e.ToRinEvent((Bot)sender);
            Task.Run(() => CommandManager.Instance.CommandInvoke(rinMessageEvent));
        }

        public void OnKonataFriendMessage(object sender, FriendMessageEvent e)
        {
            Logger.Info($"QQ DM {e.FriendUin}\n{e.Message.Chain}");
            if (e.FriendUin == (sender as Bot).Uin) return;
            var rinMessageEvent = e.ToRinEvent((Bot)sender);
            Task.Run(() => CommandManager.Instance.CommandInvoke(rinMessageEvent));
        }

        public void OnKonataGroupPoke(object sender, GroupPokeEvent e)
        {
            Logger.Info($"QQ {e.OperatorUin} {e.ActionPrefix} {e.MemberUin} {e.ActionSuffix}");
            if (e.OperatorUin == (sender as Bot).Uin) return;
            var rinMessageEvent = e.ToRinEvent((Bot)sender);
            Task.Run(() => CommandManager.Instance.CommandInvoke(rinMessageEvent));
        }

        public void OnKonataFriendPoke(object sender, FriendPokeEvent e)
        {
            Logger.Info($"QQ {e.FriendUin} {e.ActionPrefix} {e.SelfUin} {e.ActionSuffix}");
            if (e.FriendUin == (sender as Bot).Uin) return;
            var rinMessageEvent = e.ToRinEvent((Bot)sender);
            Task.Run(() => CommandManager.Instance.CommandInvoke(rinMessageEvent));
        }

        public void OnKonataBotEvent(object sender, object e)
        {

        }
    }
}
