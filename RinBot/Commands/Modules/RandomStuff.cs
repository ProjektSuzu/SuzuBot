using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using RinBot.Core.Attributes.Command.Modules;
using RinBot.Core.Attributes.CommandSet;
using RinBot.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Commands.Modules
{
    [CommandSet("随机", "com.akulak.random")]
    internal class RandomStuff : BaseCommand
    {
        [GroupMessageCommand("随机选择", new[] { @"^choose\s?([\s\S]+)?" , @"^帮我选\s?([\s\S]+)?" })]
        public void OnChoose(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            if (args.Count == 0)
            {
                messageEvent.Reply(bot, new MessageBuilder().Add(ReplyChain.Create(messageEvent.Message))
                    .Text("你什么都没说(゜-゜)让我怎么帮你啊"));
                return;
            }
            else if (args.Count == 1)
            {
                messageEvent.Reply(bot, new MessageBuilder().Add(ReplyChain.Create(messageEvent.Message))
                    .Text("你这让我有的选吗(゜-゜)"));
                return;
            }
            else
            {
                var result = args[new Random().Next(args.Count)];
                if (new Random().Next(100) == 7)
                {
                    result = "什么都不选";
                }
                messageEvent.Reply(bot, new MessageBuilder().Add(ReplyChain.Create(messageEvent.Message))
                    .Text($"铃的建议是{result}呢"));
                return;
            }
        }
    }
}
