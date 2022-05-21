using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Newtonsoft.Json;
using RinBot.Core.Attributes.Command.Modules;
using RinBot.Core.Attributes.CommandSet;
using RinBot.Core.Components;

namespace RinBot.Commands.Modules
{
    [CommandSet("戳一戳", "com.akulak.poke")]
    internal class Poke : BaseCommand
    {
        private List<string> replys;

        private string imgDir;

        public override void OnInit()
        {
            string? json = File.ReadAllText(BotManager.resourcePath + "/pokeReply.json");
            replys = JsonConvert.DeserializeObject<List<string>>(json) ?? new();

            imgDir = Path.Combine(BotManager.resourcePath, "image");
        }

        [GroupPokeCommand("戳一戳")]
        public void OnPoke(Bot bot, GroupPokeEvent pokeEvent)
        {
            if (pokeEvent.OperatorUin == bot.Uin || pokeEvent.MemberUin != bot.Uin)
            {
                return;
            }

            string? reply = replys.ElementAt(new Random().Next(replys.Count()));
            reply = reply.Replace("{uin}", pokeEvent.OperatorUin.ToString());
            reply = reply.Replace("{imgDir}", imgDir);
            try
            {
                bot.SendGroupMessage(pokeEvent.GroupUin, MessageBuilder.Eval(reply));
            }
            catch
            {

            }
            return;
        }

    }
}
