using Konata.Core.Events.Model;
using Konata.Core;
using RinBot.Core.Component.Command.CustomAttribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Konata.Core.Interfaces.Api;
using Newtonsoft.Json;
using NLog;
using RinBot.Core;
using Konata.Core.Message;

namespace RinBot.Command
{
    [Module("互动", "org.akulak.interact")]

    internal class Interact
    {
        private List<string> pokeReplys = new();

        private static readonly string POKE_REPLY_PATH = Path.Combine(Global.RESOURCE_PATH, "pokeReply.json");
        private static readonly string IMG_PATH = Path.Combine(Global.RESOURCE_PATH, "image");
        public Interact()
        {
            string? json = File.ReadAllText(POKE_REPLY_PATH);
            pokeReplys = JsonConvert.DeserializeObject<List<string>>(json ?? "") ?? new();
        }
        
        [Command("戳一戳", "", MatchingType.Always, ReplyType.Reply)]
        public void OnPing(GroupPokeEvent e, Bot bot)
        {
            if (e.MemberUin != bot.Uin || e.OperatorUin == bot.Uin) return;
            
            string reply = pokeReplys.ElementAt(new Random().Next(pokeReplys.Count()));
            reply = reply.Replace("{uin}", e.OperatorUin.ToString());
            reply = reply.Replace("{name}", bot.GetGroupMemberInfo(e.GroupUin, e.OperatorUin).Result.NickName);
            reply = reply.Replace("{imgDir}", IMG_PATH);
            try
            {
                bot.SendGroupMessage(e.GroupUin, MessageBuilder.Eval(reply));
            }
            catch
            {

            }
        }
        [Command("戳一戳", "", MatchingType.Always, ReplyType.Reply)]
        public void OnPing(FriendPokeEvent e, Bot bot)
        {
            if (e.FriendUin == bot.Uin || e.SelfUin != 0) return;

            string reply = pokeReplys.ElementAt(new Random().Next(pokeReplys.Count()));
            reply = reply.Replace("{uin}", e.FriendUin.ToString());
            reply = reply.Replace("{name}", bot.GetFriendList().Result.First(x => x.Uin == e.FriendUin).Name);
            reply = reply.Replace("{imgDir}", IMG_PATH);
            try
            {
                bot.SendFriendMessage(e.FriendUin, MessageBuilder.Eval(reply));
            }
            catch
            {
                
            }
        }
    }
}
