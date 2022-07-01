using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using RinBot.Core.Component.Command.CustomAttribute;
using RinBot.Core.Component.Event;
using RinBot.Core.Component.Message;
using RinBot.Core.Component.Message.Model;

namespace RinBot.Command
{
    [Module("Core", "org.akulak.core")]
    internal class Core
    {
        [Command("Ping", "ping", MatchingType.Exact, ReplyType.Reply)]
        public string OnPing(RinEvent e)
        {
            return "Pong!";
        }
        
        
    }
}
