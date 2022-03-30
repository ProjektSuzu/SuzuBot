using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Newtonsoft.Json;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.Components;

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
            string? json = File.ReadAllText(BotManager.resourcePath + "/pokeReply.json");
            replys = JsonConvert.DeserializeObject<List<string>>(json) ?? new();
        }

        [GroupPokeCommand("戳一戳")]
        public void OnPoke(Bot bot, GroupPokeEvent pokeEvent)
        {
            if (pokeEvent.OperatorUin == bot.Uin || pokeEvent.MemberUin != bot.Uin)
            {
                return;
            }

            string? reply = replys.ElementAt(new Random().Next(replys.Count()));
            try
            {
                if (reply.StartsWith("img://"))
                {
                    reply = reply.Substring(6);
                    reply = Path.Combine(BotManager.resourcePath, reply);
                    bot.SendGroupMessage(pokeEvent.GroupUin, new MessageBuilder().Image(reply));
                }
                else
                {
                    bot.SendGroupMessage(pokeEvent.GroupUin, new MessageBuilder(reply));
                }
            }
            catch
            {

            }
            return;
        }

    }
}
