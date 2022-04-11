#if DEBUG
using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;

namespace ProjektRin.Commands.Modules
{

    [CommandSet("Debug", "com.akulak.debug")]
    internal class DebugCommand : BaseCommand
    {
        public override void OnInit() { }

        public override string Help => "DEBUG";

        [GroupMessageCommand("Fake", @"^fake\s?([\s\S]+)?")]
        public void OnFake(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            AtChain? atChain = (AtChain?)messageEvent.Message.Chain.FirstOrDefault(x => x is AtChain);
            if (atChain == null) { return; }
            args.RemoveAt(0);
            Konata.Core.Common.BotMember? member = bot.GetGroupMemberList(messageEvent.GroupUin).Result.FirstOrDefault(x => x.Uin == atChain.AtUin);
            if (member == null) { return; }
            MultiMsgChain? multiReply = MultiMsgChain.Create();

            foreach (string? arg in args)
            {
                multiReply.AddMessage(
                    new MessageStruct(member.Uin, member.NickName, MessageBuilder.Eval(arg).Build()
                    ));
            }

            multiReply.AddMessage(
                new MessageStruct(bot.Uin, bot.Name, new MessageBuilder("这是一条伪造的合并转发信息 请勿相信上述任何人的任何话").Build()
                ));

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(multiReply));
            return;
        }

        [GroupMessageCommand("test", @"^test")]
        public void OnTest(Bot bot, GroupMessageEvent messageEvent)
        {

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(messageEvent.Message.Chain.GetChain<TextChain>()));
            return;
        }
    }
}
#endif