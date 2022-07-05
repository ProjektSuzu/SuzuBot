using RinBot.Core.Component.Command.CustomAttribute;
using RinBot.Core.Component.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Command.Arcaea
{
    [Module("Arcaea", "org.akulak.arcaea")]
    internal class Arcaea
    {
        [Command("Arcaea", new[] { @"arc\s?(.+)?", @"a\s(.+)?" }, (int)MatchingType.Regex, ReplyType.Reply)]
        public void OnArcaea(RinEvent e, List<string> args)
        {
            if (args.Count == 0)
            {
                e.Reply("Arcaea查询功能正在开发中...");
                return;
            }

            string funcName = args[0];

            switch (funcName)
            {
                default:
                    break;
            }
        }
    }
}
