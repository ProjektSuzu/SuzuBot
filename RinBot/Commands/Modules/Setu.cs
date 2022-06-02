using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using NLog;
using RinBot.Core.Attributes.Command.Modules;
using RinBot.Core.Attributes.CommandSet;
using System.Net.Http.Headers;

namespace RinBot.Commands.Modules
{
    [CommandSet("色图", "com.akulak.setu")]
    internal class Setu : BaseCommand
    {
        private static readonly string api = @"https://api.lolicon.app/setu/v2";

        private static readonly string TAG = "SETU";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        private static List<KeyValuePair<uint, DateTime>> cooldownList = new();
        private static readonly TimeSpan cooldown = TimeSpan.FromSeconds(30);

        private HttpClient httpClient = new HttpClient();
        public override void OnInit() { }

        private SetuResult GetSetu(List<string> tags, int r18 = 0, int num = 1)
        {

            string? json = JsonConvert.SerializeObject(new SetuPost(r18, tags, num));
            HttpContent content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage? response = httpClient.PostAsync(api, content).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("服务器连接失败.");
            }

            SetuResult? result = JsonConvert.DeserializeObject<SetuResult>(response.Content.ReadAsStringAsync().Result);
            if (result == null)
            {
                throw new Exception("数据转换失败.");
            }

            return result;
        }

        [GroupMessageCommand("色图", new[] { @"^setu\s?([\s\S]+)?", @"^色图\s?([\s\S]+)?" })]
        public void OnSetu(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            if (cooldownList.Any(x => x.Key == messageEvent.GroupUin))
            {
                var lastTime = cooldownList.First(x => x.Key == messageEvent.GroupUin).Value;
                if (lastTime.Add(cooldown) > DateTime.Now)
                {
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"不可以色色!\n下一次使用还要等{(cooldown - (DateTime.Now - lastTime)).Seconds}秒哦"));
                    return;
                }
            }
            cooldownList.RemoveAll(x => x.Key == messageEvent.GroupUin);

            string? reply = "";
            int r18 = 0;
            List<string> tags = new();

            string? arg = "";
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

            cooldownList.Add(new KeyValuePair<uint, DateTime>(messageEvent.GroupUin, DateTime.Now));
            reply = $"处理中 请稍候.";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text(reply));

            SetuResult result;
            try
            {
                result = GetSetu(tags, r18, 1);
            }
            catch (Exception e)
            {
                reply = $"错误: {e.Message}";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            if (result.data.Count == 0)
            {
                reply = $"找不到符合要求的色图: {string.Join(' ', tags)}";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            MultiMsgChain multiReply = MultiMsgChain.Create();
            HttpClient httpClient = new HttpClient();
            List<Task> tasks = new List<Task>();
            var data = result.data.First();

            byte[] bytes;
            try
            {
                bytes = httpClient.GetByteArrayAsync(data.urls.regular).Result;
            }
            catch (Exception)
            {
                reply = $"错误: 下载图片时发生错误.";
                MessageBuilder? errorMessage = new MessageBuilder(reply);
                multiReply
                .AddMessage(
                new MessageStruct(bot.Uin, bot.Name,
                errorMessage.Build()
                ));
                return;
            }
            reply =
             "色图来了(º﹃º )\n" + 
            $"标题: {data.title}\n" +
            $"PID: {data.pid}\n" +
            $"作者: {data.author}\n" +
            $"标签: {string.Join(' ', data.tags)}\n" +
            $"\n";
            MessageBuilder message = new MessageBuilder(reply).Image(bytes);

            multiReply
                .AddMessage(
                new MessageStruct(bot.Uin, bot.Name,
                message.Build()
                ));

            Task<bool>? success = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(multiReply));
            Logger.Info($"Setu send: {success.Result}");
            return;
        }
    }

    internal class SetuPost
    {
        public int r18;
        public List<string> tag;
        public int num;
        public string size = "regular";

        public SetuPost(int r18, List<string> tag, int num = 1)
        {
            this.r18 = r18;
            this.tag = tag;
            this.num = num;
        }

    }

    internal class SetuResult
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
