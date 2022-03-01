using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.System;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("CoreCommands")]
    internal class CoreCommandSet : BaseCommand
    {
        GroupManager groupManager;
        public override void OnInit()
        {
            groupManager = GroupManager.Instance;
        }

        [GroupMessageCommand("Ping", @"^ping")]
        public void OnPing(Bot bot, GroupMessageEvent messageEvent)
        {
            var ticksNow = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            long ticksSend = (long)messageEvent.MessageTime * 1000;
            var reply = $"Pong! ({ticksNow - ticksSend}ms)";

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }

        [GroupMessageCommand("PassiveMode", @"^passive\s?([\s\S]+)?")]
        public void OnPassiveMode(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var groupUin = messageEvent.GroupUin;
            while (args.Count > 0)
            {
                var arg = args.ElementAt(0);
                args.RemoveAt(0);

                if (uint.TryParse(arg, out var value))
                {
                    groupUin = value;
                }
            }

            var flag = groupManager.TogglePassiveMode(groupUin);

            var reply = $"G{groupUin} => Passive Mode {(flag ? "On" : "Off")}";

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }

        [GroupMessageCommand("ToggleCommandSet", @"^cmdctl\s?([\S]+)?([\s\S]+)?")]
        public void OnToggleSet(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var groupUin = messageEvent.GroupUin;
            List<string> sets = new List<string>();
            bool? action = null;
            string reply = "";
            switch (args[0])
            {
                case "enable": action = true; break;
                case "disable": action = false; break;

                default:
                    {
                        reply = $"Error: IllegalArgument: \"{args[0]}\" for <enable/disable>";
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                        return;
                    }
            }

            if (args.Count > 1)
            {
                sets = args[1].Trim().Split(' ').ToList();
                sets.RemoveAll(x => x.Trim() == "");
            }

            groupManager.SetDisabledCommandSet(groupUin, (bool)action, sets);

            reply = $"G{groupUin} => {sets.Count} CommandSet(s) {((bool)action ? "Enabled" : "Disabled")}";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
        }

    }
}
