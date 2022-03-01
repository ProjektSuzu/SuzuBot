using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("TestCommands")]
    internal class TestCommandSet : BaseCommand
    {
        public override void OnInit()
        {
        }

        [GroupMessageCommand("Test", @"^test")]
        public void OnTest(Bot bot, GroupMessageEvent messageEvent)
        {
            var reply = $"Rin desu.";

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }
    }
}
