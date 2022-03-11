using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Newtonsoft.Json;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("戳一戳", "com.akulak.poke")]
    internal class PokeCommand : BaseCommand
    {
        public override string Help =>
            $"[戳一戳]\n" +
            $"被戳一戳时随机回复一句话";

        private List<string> replys;

        public override void OnInit()
        {
            var json = File.ReadAllText(BotManager.resourcePath + "/pokeReply.json");
            replys = JsonConvert.DeserializeObject<List<string>>(json) ?? new();
        }

        [GroupPokeCommand("戳一戳")]
        public void OnPoke(Bot bot, GroupPokeEvent pokeEvent)
        {
            if (pokeEvent.OperatorUin == bot.Uin || pokeEvent.MemberUin != bot.Uin) return;
            var reply = replys.ElementAt(new Random().Next(replys.Count()));
            bot.SendGroupMessage(pokeEvent.GroupUin, new MessageBuilder(reply));
            return;
        }

    }
}
