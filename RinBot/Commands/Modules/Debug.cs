using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using ProjektRin.Core.Attributes.Command.Modules;
using ProjektRin.Core.Attributes.CommandSet;
using ProjektRin.Core.Components;
using ProjektRin.Utils;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("Debug", "com.akulak.debug")]
    internal class Debug : BaseCommand
    {
        [GroupMessageCommand("测试", Permission.Admin, new[] { @"^test$" })]
        public void OnDebug(Bot bot, GroupMessageEvent e, List<string> args)
        {
            e.Reply(bot, new MessageBuilder("I`m Rin."));
        }
    }
}
