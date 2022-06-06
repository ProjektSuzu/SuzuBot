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
using SkiaSharp;
using SkiaSharp.QrCode;

namespace RinBot.Commands.Modules
{
    [CommandSet("色图", "com.akulak.setu")]
    internal class Setu : BaseCommand
    {
        private static readonly string api = @"https://api.lolicon.app/setu/v2";

        private static readonly string TAG = "SETU";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        private static List<KeyValuePair<uint, DateTime>> cooldownList = new();
        private static readonly TimeSpan cooldown = TimeSpan.FromSeconds(10);

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
                var cdTime = cooldownList.First(x => x.Key == messageEvent.GroupUin).Value;
                if (cdTime > DateTime.Now)
                {
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                        .Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"不可以色色!\n下一次使用还要等{(int)(cdTime - DateTime.Now).TotalSeconds}秒哦"));
                    return;
                }
            }
            cooldownList.RemoveAll(x => x.Key == messageEvent.GroupUin);

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
                                reply = $"错误: 参数非法: \"{arg}\" => <num>";
                                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                                return;
                            }

                            if (num > 10)
                            {
                                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                                    .Add(ReplyChain.Create(messageEvent.Message))
                                    .Text($"不可以色色!\n最多只能获取10张色图哦"));
                                return;
                            }
                            break;
                        }

                    default:
                        {
                            tags.Add(arg);
                            break;
                        }
                }
            }

            cooldownList.Add(new KeyValuePair<uint, DateTime>(messageEvent.GroupUin, DateTime.Now + cooldown * num));
            //cooldownList.Add(new KeyValuePair<uint, DateTime>(messageEvent.GroupUin, DateTime.Now + cooldown));

            reply = $"处理中 请稍候.";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text(reply));

            SetuResult result;
            try
            {
                result = GetSetu(tags, r18, num);
            }
            catch (Exception e)
            {
                reply = $"错误: {e.Message}";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                cooldownList.RemoveAll(x => x.Key == messageEvent.GroupUin);
                return;
            }

            if (result.data.Count == 0)
            {
                reply = $"找不到符合要求的色图: {string.Join(' ', tags)}";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                cooldownList.RemoveAll(x => x.Key == messageEvent.GroupUin);
                return;
            }

            MultiMsgChain multiReply = MultiMsgChain.Create();
            HttpClient httpClient = new HttpClient();
            List<Task> tasks = new List<Task>();
            //var data = result.data.First();

            void DownloadPic(SetuResult.Data data)
            {
                reply = "";
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

                var image = ImageChain.Create(bytes);
                bot.UploadGroupImage(image, messageEvent.GroupUin).Wait();
                using (var generator = new QRCodeGenerator())
                {
                    var qr = generator.CreateQrCode(image.ImageUrl, ECCLevel.L);
                    var info = new SKImageInfo(512, 512);
                    using (var surface = SKSurface.Create(info))
                    {
                        var canvas = surface.Canvas;
                        canvas.Render(qr, info.Width, info.Height);

                        using (var qrImage = surface.Snapshot())
                        using (var encode = qrImage.Encode(SKEncodedImageFormat.Png, 100))
                        {
                            bytes = encode.ToArray();
                        }
                    }
                }

                MessageBuilder message = new MessageBuilder(reply).Image(bytes);

                multiReply
                    .AddMessage(
                    new MessageStruct(bot.Uin, bot.Name,
                    message.Build()
                    ));
            }

            int count = 0;
            foreach (var data in result.data)
            {
                Task? task = new Task(() => DownloadPic(data));
                task.Start();
                tasks.Add(task);
                count++;
            }

            Task.WaitAll(tasks.ToArray());
            //cooldownList.RemoveAll(x => x.Key == messageEvent.GroupUin);
            //cooldownList.Add(new KeyValuePair<uint, DateTime>(messageEvent.GroupUin, DateTime.Now + cooldown * count));

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
