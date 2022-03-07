#if DEBUG
using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;

namespace ProjektRin.Commands.Modules
{

    [CommandSet("Debug")]
    internal class DebugCommands : BaseCommand
    {
        public override void OnInit() { }

        public override string Help => "DEBUG";

        [GroupMessageCommand("Fake", @"^fake\s?([\s\S]+)?")]
        public void OnFake(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var atChain = (AtChain?)messageEvent.Message.FirstOrDefault(x => x is AtChain);
            if (atChain == null) { return; }
            args.RemoveAt(0);
            var member = bot.GetGroupMemberList(messageEvent.GroupUin).Result.FirstOrDefault(x => x.Uin == atChain.AtUin);
            if (member == null) { return; }
            var multiReply = MultiMsgChain.Create();

            foreach (var arg in args)
            {
                multiReply.AddMessage(
                    new SourceInfo(member.Uin, member.NickName), MessageBuilder.Eval(arg)
                    );
            }

            multiReply.AddMessage(
                new SourceInfo(bot.Uin, bot.Name), new MessageBuilder("这是一条伪造的合并转发信息 请勿相信上述任何人的任何话")
                );

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(multiReply));
            return;
        }
    }
}
#endif