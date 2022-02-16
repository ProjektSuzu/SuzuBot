using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjektRin.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektRin.Commands.Models
{
    [CommandSet("ArcaeaCommandSet")]
    internal class FinDDCommand : BaseCommand
    {
        private static HttpClient _httpClient;

        public override void OnInit()
        {
            _httpClient = new HttpClient();

        }

        [GroupMessageCommand(
            "findd",
            "查看指定用户的Bilibili关注列表内有多少Vtb",
            "/findd <用户名/UID>\n" +
            "/查成分 <用户名/UID>",
            new []
            {
                @"^findd\s?([\S]+)?",
                @"^查成分\s?([\S]+)?"
            }
            )]
        public void OnFinDD(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            if (args == null || args.Count == 0)
            {
                _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("未指定的参数: <用户名/UID>"));
                return;
            }

            _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("获取中, 请稍后..."));


            var target = args.FirstOrDefault();
            var url = $"https://api.asoulfan.com/cfj/?name={target}";
            var response = _httpClient.GetAsync(url).Result;

            FinDDResult result;

            if (!response.IsSuccessStatusCode)
            {
                _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("远端服务器发生通信错误"));
                return;
            }

            try
            {
                result = JsonConvert.DeserializeObject<FinDDResult>(response.Content.ReadAsStringAsync().Result);
            }
            catch 
            {
                _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("远端服务器发生通信错误"));
                return;
            }

            if (result == null || result.data.list.Count == 0)
            {
                _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .At(messageEvent.MemberUin)
                    .PlainText($"\n{target} 没有关注任何管人")
                    );
                return;
            }

            var nameList = result.data.list.Select(x => x.uname).ToList();
            var count = nameList.Count;
            var reply = String.Join("、", nameList);
            _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .At(messageEvent.MemberUin)
                    .PlainText($"\n{target} 关注了 {count} 个管人:\n" +
                    $"{reply}")
                    );
        }
    }

    class FinDDResult
    {
        public int code;
        public string message;
        public int ttl;
        public Data data;

        public class Data
        {
            public List<BUser> list;
            public class BUser
            {
                public string uname;
            }
        }
    }
}
