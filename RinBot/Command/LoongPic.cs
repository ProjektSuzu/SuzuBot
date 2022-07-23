using RinBot.Core;
using RinBot.Core.Component.Command.CustomAttribute;
using RinBot.Core.Component.Event;
using RinBot.Core.Component.Message;
using RinBot.Core.Component.Message.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Command
{
    [Module("龙图", "org.akulak.loongPic", ModuleEnableConfig.NormallyDisable)]
    internal class LoongPic
    {
        private static readonly string LOONG_PIC_PATH = Path.Combine(Global.RESOURCE_PATH, "LoongPic");


        [Command("发龙图", new[] { "龙图", "loong", "dragon"}, (int)MatchingType.Contains | (int)MatchingType.NoLeadChar, ReplyType.Send)]
        public RinMessageChain OnLoongPic(RinEvent e)
        {
            var chains = new RinMessageChain();
            var files = Directory.EnumerateFiles(LOONG_PIC_PATH);
            var img = files.ElementAt(new Random().Next(files.Count()));
            chains.Add(ImageChain.Create(File.ReadAllBytes(img)));
            return chains;
        }
    }
}
