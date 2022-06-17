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
    [CommandSet("互动", "com.akulak.interact")]
    internal class Interact : BaseCommand
    {
        private List<string> pokeReplys;

        private string imgDir;

        public override void OnInit()
        {
            string? json = File.ReadAllText(BotManager.resourcePath + "/pokeReply.json");
            pokeReplys = JsonConvert.DeserializeObject<List<string>>(json) ?? new();

            imgDir = Path.Combine(BotManager.resourcePath, "image");
        }

        [GroupPokeCommand("戳一戳")]
        public void OnPoke(Bot bot, GroupPokeEvent pokeEvent)
        {
            if (pokeEvent.OperatorUin == bot.Uin || pokeEvent.MemberUin != bot.Uin)
            {
                return;
            }

            string? reply = pokeReplys.ElementAt(new Random().Next(pokeReplys.Count()));
            reply = reply.Replace("{uin}", pokeEvent.OperatorUin.ToString());
            reply = reply.Replace("{name}", bot.GetGroupMemberInfo(pokeEvent.GroupUin, pokeEvent.OperatorUin).Result.NickName);
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
