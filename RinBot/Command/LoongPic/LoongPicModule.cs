using Konata.Core.Message;
using RestSharp;
using RinBot.Core;
using RinBot.Core.Components;
using RinBot.Core.Components.Attributes;
using RinBot.Core.KonataCore.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Command.Setu
{
    [Module("龙图", "AkulaKirov.LoongPic", enableType: ModuleEnableType.NormallyDisabled)]
    internal class LoongPicModule
    {
        public LoongPicModule()
        {
            loongImgs = Directory.EnumerateFiles(RESOURCE_DIR_PATH).ToArray();
        }
        private static readonly string RESOURCE_DIR_PATH = Path.Combine(GlobalScope.RESOURCE_DIR_PATH, "AkulaKirov.LoongPic");
        private static string[] loongImgs = Array.Empty<string>();


        [TextCommand("发龙图", new[] { "loong", "dragon", "龙图" })]
        public void OnSendLoongPic(MessageEventArgs messageEvent)
        {
            var img = loongImgs[new Random().Next(loongImgs.Length)];
            messageEvent.Subject.SendMessage(new MessageBuilder().Image(File.ReadAllBytes(img)).Build());
        }
    }
}
