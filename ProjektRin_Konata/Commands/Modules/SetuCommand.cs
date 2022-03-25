using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using NLog;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using System.Net.Http.Headers;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("色图", "com.akulak.setu")]
    internal class SetuCommand : BaseCommand
    {
        private static readonly string api = @"https://api.lolicon.app/setu/v2";

        private static readonly string TAG = "SETU";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public override string Help => $"[色图]\n" +
                $"/setu [-r18] [-n <Num>] [<tag>...]      获取色图\n" +
                $"\n" +
                $"  -h          显示帮助信息\n" +
                $"  -r18        R18开关 (慎用)\n" +
                $"  -n <num>    指定要获取的数量 默认为1\n" +
                $"              实际数量可能会根据返回图片数量而变化\n" +
                $"\n" +
                $"  tag         指定图片的标签 按空格分开 最多3个\n" +
                $"快捷名:\n" +
                $"/色图" +
                $"";
        public override void OnInit() { }

        private SetuResult GetSetu(List<string> tags, int r18 = 0, int num = 1)
        {
            HttpClient _httpClient = new HttpClient();
            string? json = JsonConvert.SerializeObject(new SetuPost(r18, tags, num));
            HttpContent content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage? response = _httpClient.PostAsync(api, content).Result;
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
            string? reply = "";
            int r18 = 0;
            int num = 1;
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

                    case "-n":
                        {
                            arg = args.FirstOrDefault(defaultValue: "");
                            if (args.Count > 0)
                            {
                                args.RemoveAt(0);
                            }

                            if (!int.TryParse(arg, out num) || num < 1)
                            {
                                reply = $"错误: 参数非法: \"{arg}\" => -n <num>";
                                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                                return;
                            }
                            break;
                        }

                    case "-h":
                        {
                            reply = Help;
                            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                            return;
                        }

                    default:
                        {
                            tags.Add(arg);
                            break;
                        }
                }
            }

            reply = $"处理中 请稍候.";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));

            SetuResult result;
            try
            {
                result = GetSetu(tags, r18, num);
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

            List<Task> tasks = new List<Task>();

            void DownloadPic(SetuResult.Data data)
            {
                HttpClient httpClient = new HttpClient();
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
                $"色图来了(º﹃º )\n" +
                $"标题: {data.title}\n" +
                $"作者: {data.author}\n" +
                $"PID: {data.pid}\n" +
                $"标签: {string.Join(' ', data.tags)}\n" +
                $"\n";
                MessageBuilder message = new MessageBuilder(reply).Image(bytes);

                multiReply
                    .AddMessage(
                    new MessageStruct(bot.Uin, bot.Name,
                    message.Build()
                    ));
            }

            foreach (SetuResult.Data? data in result.data)
            {
                Task? task = new Task(() => DownloadPic(data));
                task.Start();
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            var success = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(multiReply));
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
