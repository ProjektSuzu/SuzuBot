using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using ProjektRin.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektRin.Commands.Models
{
    [CommandSet("LoongPicCommandSet")]
    internal class LoongPicCommands : BaseCommand
    {
        private string rootPath;
        private string resourcePath;
        private string loongPath;
        private DirectoryInfo picDir;

        public override void OnInit() 
        {
            rootPath = AppDomain.CurrentDomain.BaseDirectory;
            resourcePath = Path.Combine(rootPath, "resources");
            loongPath = Path.Combine(resourcePath, "LoongPic");
            picDir = new DirectoryInfo(loongPath);
        }

        [GroupMessageCommand(
            "loongPic",
            "发点龙图",
            "/loong\n" +
            "/龙图",
            new[]
            {
                @"^loong\s?([\S]+)?",
                @"^龙图\s?([\S]+)?"
            }
            )]
        public void OnLoongPic(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            Random random = new Random();
            var pic = picDir.GetFiles()[random.Next(picDir.GetFiles().Length)];
            var reply = new MessageBuilder()
                .Image(pic.FullName);

            _ = bot.SendGroupMessage(messageEvent.GroupUin, reply);
        }
    }
}
