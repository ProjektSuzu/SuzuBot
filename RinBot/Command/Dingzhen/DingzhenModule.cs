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
    [Module("一眼丁真", "AkulaKirov.Dingzhen", enableType: ModuleEnableType.NormallyDisabled)]
    internal class DingzhenModule
    {
        public DingzhenModule()
        {
            dingzhenImgs = Directory.EnumerateFiles(RESOURCE_DIR_PATH).ToArray();
        }
        private static readonly string RESOURCE_DIR_PATH = Path.Combine(GlobalScope.RESOURCE_DIR_PATH, "AkulaKirov.Dingzhen");
        private static string[] dingzhenImgs = Array.Empty<string>();


        [TextCommand("发一眼丁真图", new[] { "dingzhen", "丁真" })]
        public void OnSendDingzhen(MessageEventArgs messageEvent)
        {
            var img = dingzhenImgs[new Random().Next(dingzhenImgs.Length)];
            messageEvent.Subject.SendMessage(new MessageBuilder().Image(File.ReadAllBytes(img)).Build());
        }
    }
}
