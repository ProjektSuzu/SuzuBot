using RinBot.Core.Components;
using RinBot.Core.Components.Attributes;

namespace RinBot.Command
{
    [Module("核心功能", "org.akulak.core", true)]
    internal class Core
    {
        [Command("Ping", "ping")]
        public void OnPing(MessageEventArgs messageEvent)
        {
            messageEvent.Reply("=^•-•^=");
        }
    }
}
