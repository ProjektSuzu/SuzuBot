using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("SetuCommands")]
    internal class SetuCommandSet : BaseCommand
    {
        private HttpClient _httpClient;

        private static string api = @"https://api.lolicon.app/setu/v2";
        public override void OnInit() 
        {
            _httpClient = new HttpClient();
        }

        private SetuResult GetSetu(List<string> tags, int r18 = 0)
        {
            var json = JsonConvert.SerializeObject(new SetuPost(r18, tags));
            HttpContent content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = _httpClient.PostAsync(api, content).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("服务器连接失败.");
            }

            var result = JsonConvert.DeserializeObject<SetuResult>(response.Content.ReadAsStringAsync().Result);
            if (result == null)
            {
                throw new Exception("数据转换失败.");
            }

            return result;
        }

        [GroupMessageCommand("Setu", new[] { @"^setu\s?([\s\S]+)?" , @"^色图\s?([\s\S]+)?" })]
        public void OnSetu(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var reply = "";
            int r18 = 0;
            List<string> tags = new();

            var arg = "";
            while (args.Count > 0)
            {
                arg = args[0];
                args.RemoveAt(0);

                switch (arg)
                {
                    case "-r18":
                        {
                            r18 = 2;
                            break;
                        }

                    default:
                        {
                            tags.Add(arg);
                            break;
                        }
                }
            }

            reply = $"处理中 请稍后.";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));

            SetuResult result;
            try
            {
                result = GetSetu(tags, r18);
            }
            catch (Exception e)
            {
                reply = $"错误: {e.Message}";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            if (result.data.Count == 0)
            {
                reply = $"找不到符合要求的色图: {String.Join(' ', tags)}";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            var data = result.data[0];
            byte[] bytes;
            try
            {
                bytes = _httpClient.GetByteArrayAsync(data.urls.regular).Result;
            }
            catch (Exception e)
            {
                reply = $"错误: 下载图片时发生错误.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            reply =
                $"es ein Bild für dich (º﹃º )\n" +
                $"标题: {data.title}\n" +
                $"作者: {data.author}\n" +
                $"PID: {data.pid}\n" +
                $"标签: {String.Join(' ', data.tags)}\n" +
                $"\n";

            

            var message = new MessageBuilder(reply);
            message.Image(bytes);

            var multiReply = MultiMsgChain.Create()
                .AddMessage(
                new SourceInfo(bot.Uin, bot.Name),
                message
                );

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(multiReply));
            return;
        }
    }
    class SetuPost
    {
        public int r18;
        public List<string> tag;
        public string size = "regular";

        public SetuPost(int r18, List<string> tag)
        {
            this.r18 = r18;
            this.tag = tag;
        }

    }

    class SetuResult
    {
        public string error;
        public List<Data> data;
        public class Data
        {
            public int pid;
            public int uid;
            public string title;
            public string author;
            public bool r18;
            public List<string> tags;
            public Url urls;
            public class Url
            {
                public string regular;
            }
        }
    }
}
