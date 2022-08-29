using Konata.Core.Message;
using RestSharp;
using RinBot.Core;
using RinBot.Core.Components.Attributes;
using RinBot.Core.KonataCore.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Command.Setu
{
    [Module("ACG色图", "AkulaKirov.Setu")]
    internal class SetuModule
    {
        public SetuModule()
        {
            RestClient = new RestClient(@"https://www.loliapi.com/acg/");
        }
        private static RestClient RestClient;

        private const int COST_PER_IMG_RC = 10;

        [TextCommand("ACG色图", "acg")]
        public void OnACGPic(MessageEventArgs messageEvent)
        {
            var info = GlobalScope.PermissionManager.GetUserInfo(messageEvent.Sender.Uin);
            if (info.Coin < COST_PER_IMG_RC)
            {
                messageEvent.Reply(new MessageBuilder($"[ACG]\n你的RC不足QAQ\n需要 {COST_PER_IMG_RC} RC\n你只有 {info.Coin} RC"));
                return;
            }
            else
            {
                info.Coin -= COST_PER_IMG_RC;
                GlobalScope.PermissionManager.UpdateUserInfo(info);
                var request = new RestRequest();
                var response = RestClient.Get(request);
                messageEvent.Reply(new MessageBuilder("[ACG]").Image(response.RawBytes));
                return;
            }
            
        }
    }
}
