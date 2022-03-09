using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("随机数", "com.akulak.random")]
    public class RandomChooseCommand : BaseCommand
    {
        public override string Help =>
            "";

        public override void OnInit()
        {

        }

        [GroupMessageCommand("随机选择", new[] { @"^random\s?([\s\S]+)?", @"^帮我选\s?([\s\S]+)?" })]
        public void OnRandomChoose(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var reply = "";
            var random = new Random().Next(99);
            if (random == 49)
            {
                reply = "\n铃建议什么都不做呢";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder().At(messageEvent.MemberUin).Text(reply));
                return;
            }
            reply = "\n铃的建议是: ";
            reply += args[new Random().Next(args.Count())];
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder().At(messageEvent.MemberUin).Text(reply));
            return;
        }

    }
}
