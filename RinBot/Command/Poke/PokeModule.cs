using Konata.Core.Message;
using Newtonsoft.Json;
using RinBot.Core;
using RinBot.Core.Components.Attributes;
using RinBot.Core.KonataCore.Events;

namespace RinBot.Command.Poke
{
    [Module("戳一戳", "AkulaKirov.Poke")]
    internal class PokeModule
    {
        private static readonly string RESOURCE_DIR_PATH = Path.Combine(GlobalScope.RESOURCE_DIR_PATH, "AkulaKirov.Poke");
        private static readonly string POKE_REPLY_PATH = Path.Combine(RESOURCE_DIR_PATH, "pokeReplys.json");

        public PokeModule()
        {
            Directory.CreateDirectory(RESOURCE_DIR_PATH);
            if (!File.Exists(POKE_REPLY_PATH))
            {
                pokeReplys = Array.Empty<string>();
            }
            else
            {
                pokeReplys = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(POKE_REPLY_PATH))
                             ?? Array.Empty<string>();
            }
        }

        private readonly string[] pokeReplys;

        [PokeCommand("戳一戳回复", RinBot.Core.Components.PokeReceiveTarget.Bot)]
        public void OnPokeReply(PokeEventArgs pokeEvent)
        {
            var reply = pokeReplys[new Random().Next(pokeReplys.Length)];
            pokeEvent.Subject.SendMessage(MessageBuilder.Eval(reply).Build());
        }
    }
}
