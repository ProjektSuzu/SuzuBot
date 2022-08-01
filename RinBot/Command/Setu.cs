using Newtonsoft.Json;
using RinBot.Core;
using RinBot.Core.Component.Command.CustomAttribute;
using RinBot.Core.Component.ENV;
using RinBot.Core.Component.Event;
using RinBot.Core.Component.Message;
using RinBot.Core.Component.Message.Model;
using System.Net.Http.Headers;

namespace RinBot.Command
{
    //[Module("色图", "org.akulak.setu")]
    internal class Setu
    {
        private static readonly string SETU_PATH = Path.Combine(Global.RESOURCE_PATH, "Setu");
        private static readonly string IMG_HOST_CONFIG_PATH = Path.Combine(SETU_PATH, "img_config.json");

        private const string SETU_LIMIT = "SETU_LIMIT";
        private const string SETU_COOLDOWN_SECOND_PER_IMG = "SETU_COOLDOWN_SECOND_PER_IMG";

        private const string setuApi = @"https://api.lolicon.app/setu/v2";
        private const string acgApi = @"https://www.loliapi.com/acg/";

        public Setu()
        {
            if (!Directory.Exists(SETU_PATH))
                Directory.CreateDirectory(SETU_PATH);
            if (!File.Exists(IMG_HOST_CONFIG_PATH))
                File.Create(IMG_HOST_CONFIG_PATH);
            config = JsonConvert.DeserializeObject<ImageHostConfig>(File.ReadAllText(IMG_HOST_CONFIG_PATH)) ?? new ImageHostConfig();
        }

        private int setuLimit
        {
            get
            {
                return int.Parse(EnvManager.Instance.GetEnv(SETU_LIMIT).FirstOrDefault() ?? "10");
            }
        }
        private int coolDownSeconds
        {
            get
            {
                return int.Parse(EnvManager.Instance.GetEnv(SETU_COOLDOWN_SECOND_PER_IMG).FirstOrDefault() ?? "15");
            }
        }



        private ImageHostConfig config;
        private HttpClient httpClient = new();
        private List<Cooldown> cooldowns = new List<Cooldown>();

        [Command("二次元图", new[] { @"^acg", @"^二次元" }, (int)MatchingType.Regex, ReplyType.Reply)]
        public RinMessageChain OnACG(RinEvent e)
        {
            var bytes = httpClient.GetAsync(acgApi).Result.Content.ReadAsByteArrayAsync().Result;
            var chains = new RinMessageChain();
            chains.Add(TextChain.Create("[ACG]")).Add(ImageChain.Create(bytes));
            return chains;
        }

        private UploadResult UploadImage(string url)
        {
            var data = new MultipartFormDataContent();
            data.Add(new StringContent(url), "source");
            var content = httpClient.PostAsync(config.API, data).Result.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<UploadResult>(content) ?? new() { StatusCode = 404 };
            return result;
        }

        private SetuResult GetSetu(List<string> tags, int r18 = 0, int num = 1)
        {

            string? json = JsonConvert.SerializeObject(new SetuPost(r18, tags, num));
            HttpContent content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage? response = httpClient.PostAsync(setuApi, content).Result;
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

        [Command("Pixiv色图", new[] { @"^setu\s?(.+)?", @"^色图\s?(.+)?" }, (int)MatchingType.Regex, ReplyType.Reply, eventSourceMask: 0b01)]
        public Konata.Core.Message.MessageChain OnSetu(RinEvent e, List<string> args)
        {
            var cooldown = cooldowns.FirstOrDefault(x => x.Id == e.SubjectId && x.SubjectType == e.EventSubjectType && x.dateTime >= DateTime.Now);
            var chains = new Konata.Core.Message.MessageBuilder();
            if (cooldown != null)
            {
                int seconds = (int)Math.Round((cooldown.dateTime - DateTime.Now).TotalSeconds);
                chains.Text($"[Setu]\n不可以色色!\n还需要等待 {seconds} 秒");
                return chains.Build();
            }
            cooldowns.RemoveAll(x => x.Id == e.SubjectId && x.SubjectType == e.EventSubjectType);

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
                                chains.Text($"[Setu]\n参数非法: \"{arg}\" => <num>");
                                return chains.Build();
                            }

                            if (num > setuLimit)
                            {
                                chains.Text($"[Setu]\n不可以色色!\n最多只能获取 {setuLimit} 张色图");
                                return chains.Build();
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

            cooldowns.Add(new Cooldown() { Id = e.SubjectId, SubjectType = e.EventSubjectType, dateTime = DateTime.Now.AddSeconds(coolDownSeconds * num) });

            SetuResult result;
            try
            {
                result = GetSetu(tags, r18, num);
            }
            catch (Exception exception)
            {
                chains.Text($"[Setu]\n错误: {exception.Message}");
                cooldowns.RemoveAll(x => x.Id == e.SubjectId && x.SubjectType == e.EventSubjectType);
                return chains.Build();
            }

            if (result.data.Count == 0)
            {
                chains.Text($"[Setu]\n找不到符合要求的色图: {string.Join(' ', tags)}");
                cooldowns.RemoveAll(x => x.Id == e.SubjectId && x.SubjectType == e.EventSubjectType);
                return chains.Build();
            }

            Konata.Core.Message.Model.MultiMsgChain multiReply = Konata.Core.Message.Model.MultiMsgChain.Create();
            var bot = e.OriginalSender as Konata.Core.Bot;

            void GenerateMessage(SetuResult.Data data)
            {
                var result = UploadImage(data.urls.original);

                var text = "[Setu]" + "\n(º﹃º )色图来了\n\n" +
                $"标题: {data.title}\n" +
                $"作者: {data.author}\n" +
                $"PID: {data.pid}\n" +
                $"标签: {string.Join(' ', data.tags)}\n\n" +
                $"{result.Image?.Url ?? data.urls.original}\n";

                Konata.Core.Message.MessageBuilder message = new Konata.Core.Message.MessageBuilder().Text(text);

                //using (var generator = new QRCodeGenerator())
                //{
                //    var url = result.Image?.Url ?? data.urls.original;
                //    var qr = generator.CreateQrCode(url, ECCLevel.L);
                //    var info = new SKImageInfo(512, 512);
                //    using (var surface = SKSurface.Create(info))
                //    {
                //        var canvas = surface.Canvas;
                //        canvas.Render(qr, info.Width, info.Height);

                //        using (var qrImage = surface.Snapshot())
                //        using (var encode = qrImage.Encode(SKEncodedImageFormat.Jpeg, 10))
                //        {
                //            message.Image(encode.ToArray());
                //        }
                //    }
                //}

                multiReply
                    .AddMessage(
                    new Konata.Core.Message.MessageStruct(bot.Uin, bot.Name,
                    message.Build()
                    ));
            }

            List<Task> actions = new List<Task>();
            foreach (var data in result.data)
            {
                var task = new Task(() => GenerateMessage(data));
                task.Start();
                actions.Add(task);
            }
            Task.WaitAll(actions.ToArray());

            chains.Add(multiReply);
            return chains.Build();
        }
    }

    class Cooldown
    {
        public string Id;
        public EventSubjectType SubjectType;
        public DateTime dateTime;
    }

    class ImageHostConfig
    {
        [JsonProperty("api")]
        public string API;
    }

    internal class SetuPost
    {
        public int r18;
        public List<string> tag;
        public int num;
        public List<string> size = new()
        {
            "regular",
            "original"
        };

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
                public string original;
            }
        }
    }

    class UploadResult
    {
        [JsonProperty("status_code")]
        public int StatusCode;

        [JsonProperty("image")]
        public Image Image;
    }

    class Image
    {
        [JsonProperty("url")]
        public string Url;
    }
}
