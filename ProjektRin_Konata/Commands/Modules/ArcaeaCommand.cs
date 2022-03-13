using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Newtonsoft.Json;
using NLog;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("Arcaea", "com.akulak.arcaea")]
    internal class ArcaeaCommand : BaseCommand
    {
        private string pythonPath;
        private Process python;
        private HttpClient _httpClient;

        private List<ArcaeaUserInfo> userInfos;

        private static string TAG = "ARCAEA";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public override string Help => $"[Arcaea]\n" +
                $"/arc      打印帮助信息\n" +
                $"/arc b30 [<usercode>] [-a <api>]       获取b30成绩图\n" +
                $"/arc recent [<usercode>] [-a <api>]    获取最近一次游玩成绩图\n" +
                $"/arc bind <usercode>      为当前QQ号绑定好友代码\n" +
                $"/arc unbind       为当前QQ号解绑好友代码\n" +
                $"/arc song <song>  查询一首歌的信息\n" +
                $"\n" +
                $"  -a <api>    指定使用的API\n" +
                $"              1: ArcaeaUnlimitedAPI   (默认 推荐)\n" +
                $"              2: redive.estertion.win (很慢 较稳定)\n" +
                $"\n" +
                $"  usercode    Arcaea好友代码 必须是9位纯数字\n" +
                $"  song        歌曲名字 可以是歌曲全名、内部sid、或者别名";

        public override void OnDisable()
        {
            if (python != null)
                python.Kill();
            base.OnDisable();
        }
        public override void OnInit()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = new TimeSpan(0, 5, 0);

            pythonPath = Path.Combine(BotManager.resourcePath, "ArcaeaProbe_Rework/main.py");

            python = new Process();
            python.StartInfo.UseShellExecute = false;
            python.StartInfo.CreateNoWindow = true;
            python.StartInfo.FileName = "python";
            python.StartInfo.Arguments = pythonPath;

            python.StartInfo.RedirectStandardOutput = true;
            python.OutputDataReceived += new DataReceivedEventHandler((sender, e) => Logger.Info(e.Data));

            AppDomain.CurrentDomain.ProcessExit += (s, e) => python.Kill();
#if DEBUG
            python.Start();
            python.BeginOutputReadLine();
#endif

            try
            {
                LoadUserInfo();
            }
            catch { }
            finally { SaveUserInfo(); }
        }

        private void LoadUserInfo()
        {
            var json = File.ReadAllText(BotManager.resourcePath + "/arcaea.json");
            userInfos = JsonConvert.DeserializeObject<List<ArcaeaUserInfo>>(json) ?? new();
        }

        private void SaveUserInfo()
        {
            var json = JsonConvert.SerializeObject(userInfos);
            File.WriteAllText(BotManager.resourcePath + "/arcaea.json", json, Encoding.UTF8);
        }

        [GroupMessageCommand("Arcaea", new[] { @"^arc\s?([\s\S]+)?", @"^a\s?([\s\S]+)?" })]
        public void OnArcaea(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var funcName = args.FirstOrDefault();
            var reply = "";

            if (funcName == null)
            {
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(Help));
                return;
            }

            args = args.Skip(1).ToList();
            switch (funcName)
            {
                case "b30":
                    {
                        OnB30(bot, messageEvent, args);
                        break;
                    }

                case "bind":
                    {
                        OnBind(bot, messageEvent, args);
                        break;
                    }

                case "unbind":
                    {
                        OnUnbind(bot, messageEvent, args);
                        break;
                    }

                case "song":
                    {
                        OnSongInfo(bot, messageEvent, args);
                        break;
                    }

                case "recent":
                    {
                        OnRecent(bot, messageEvent, args);
                        break;
                    }

                default:
                    {
                        reply = $"错误: 找不到功能: \"{funcName}\"\n" +
                            $"如需查看功能帮助 请输入\n" +
                            $"/arc";
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                        return;
                    }
            }
        }

        private void OnRecent(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var reply = "";
            var usercode = "";
            var api = "1";

            var arg = "";
            while (args.Count > 0)
            {
                arg = args[0];
                args.RemoveAt(0);

                switch (arg)
                {
                    case "-a":
                        {
                            api = args.FirstOrDefault(defaultValue: "");
                            if (args.Count > 0) args.RemoveAt(0);

                            if (api == "")
                            {
                                reply = $"错误: 缺少参数: -a <api>.";
                                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                                return;
                            }
                            break;
                        }

                    default:
                        {
                            usercode = arg;
                            break;
                        }
                }
            }

            if (usercode != "")
            {
                var regex = new Regex(@"[0-9]{9}");
                var match = regex.Match(usercode);
                if (!match.Success)
                {
                    reply = $"错误: 参数非法: \"{usercode}\" => [<Usercode>].";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
            }
            else
            {
                var info = userInfos.FirstOrDefault(x => x.QQUin == messageEvent.MemberUin);
                if (info == null)
                {
                    reply =
                        $"错误: 当前QQ号不存在绑定的记录.\n" +
                        $"若要使用此功能, 请先使用 /arc bind <Usercode> 进行绑定\n" +
                        $"或者直接使用 /arc b30 [<Usercode>] [-a <API>] 并指定 [<Usercode>].";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
                else
                {
                    usercode = info.Usercode;
                }
            }

            switch (api)
            {
                case "1": api = "BAA"; break;
                case "2": api = "esterTion"; break;

                default:
                    {
                        reply = $"错误: 参数非法: \"{api}\" => [-a <api>].";
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                        return;
                    }
            }

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .At(messageEvent.MemberUin)
                .Text("\n收到, 正在处理成绩图...")
                );
            var (message, bytes) = GetRecentGraph(usercode, api);
            if (message != "")
            {
                reply = $"错误: {message}";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .At(messageEvent.MemberUin)
                    .Text(reply)
                    );
                return;
            }

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .At(messageEvent.MemberUin)
                    .Image(bytes!)
                    );
            return;
        }

        private void OnSongInfo(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var reply = "";
            var sid = args.FirstOrDefault(defaultValue: "");
            if (sid == null || sid == "")
            {
                reply = $"错误: 缺少参数: <songName>.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            HttpResponseMessage response;
            try
            {
                response = _httpClient.GetAsync($"http://127.0.0.1:6002/song?sid={sid}").Result;
            }
            catch (Exception e)
            {
                reply = $"错误: {e.Message}.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                reply = $"错误: 服务器内部错误.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            var result = JsonConvert.DeserializeObject<SongResult>(response.Content.ReadAsStringAsync().Result);
            if (result == null)
            {
                reply = $"错误: 数据转换失败.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            if (result.code != 0)
            {
                reply = $"错误: 找不到歌曲 \"{sid}\".";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            reply = $"曲名: {result.data.song_name}\n" +
                $"曲师: {result.data.artist}\n" +
                $"     PST/PRS/FTR/BYD\n" +
                $"定数:{String.Join('/', result.data.info.OrderBy(x => x.difficulty).Select(x => x.rating == -1 ? "-" : $"{((float)x.rating / 10)}"))}\n" +
                $"物量:{String.Join('/', result.data.info.OrderBy(x => x.difficulty).Select(x => x.note == -1 ? "-" : x.note.ToString()))}";
            var img = Convert.FromBase64String(result.data.song_cover);

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder().Image(img).Text(reply));
            return;

        }

        private void OnB30(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var reply = "";
            var usercode = "";
            var api = "1";

            var arg = "";
            while (args.Count > 0)
            {
                arg = args[0];
                args.RemoveAt(0);

                switch (arg)
                {
                    case "-a":
                        {
                            api = args.FirstOrDefault(defaultValue: "");
                            if (args.Count > 0) args.RemoveAt(0);

                            if (api == "")
                            {
                                reply = $"错误: 缺少参数: -a <api>.";
                                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                                return;
                            }
                            break;
                        }

                    default:
                        {
                            usercode = arg;
                            break;
                        }
                }
            }

            if (usercode != "")
            {
                var regex = new Regex(@"[0-9]{9}");
                var match = regex.Match(usercode);
                if (!match.Success)
                {
                    reply = $"错误: 参数非法: \"{usercode}\" => [<Usercode>].";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
            }
            else
            {
                var info = userInfos.FirstOrDefault(x => x.QQUin == messageEvent.MemberUin);
                if (info == null)
                {
                    reply =
                        $"错误: 当前QQ号不存在绑定的记录.\n" +
                        $"若要使用此功能, 请先使用 /arc bind <Usercode> 进行绑定\n" +
                        $"或者直接使用 /arc b30 [<Usercode>] [-a <API>] 并指定 [<Usercode>].";
                    bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                    return;
                }
                else
                {
                    usercode = info.Usercode;
                }
            }

            switch (api)
            {
                case "1": api = "BAA"; break;
                case "2": api = "esterTion"; break;

                default:
                    {
                        reply = $"错误: 参数非法: \"{api}\" => [-a <api>].";
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                        return;
                    }
            }

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .At(messageEvent.MemberUin)
                .Text("\n收到, 正在处理成绩图...")
                );

            var (message, bytes) = GetB30Graph(usercode, api);
            if (message != "")
            {
                reply = $"错误: {message}";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .At(messageEvent.MemberUin)
                    .Text(reply)
                    );
                return;
            }

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                    .At(messageEvent.MemberUin)
                    .Image(bytes!)
                    );
            return;
        }

        private (string, byte[]?) GetB30Graph(string userCode, string api)
        {
            HttpResponseMessage response;
            try
            {
                response = _httpClient.GetAsync($"http://127.0.0.1:6002/getB30?usercode={userCode}&api={api}").Result;
            }
            catch (Exception e) { return (e.Message, null); }

            if (!response.IsSuccessStatusCode)
            {
                return ("服务器内部错误.", null);
            }

            var result = JsonConvert.DeserializeObject<ImgResult>(response.Content.ReadAsStringAsync().Result);
            if (result == null || result.code != 0)
            {
                return (result?.message ?? "数据转换失败.", null);
            }

            byte[] bytes = Convert.FromBase64String(result.data.img);
            return ("", bytes);
        }

        private (string, byte[]?) GetRecentGraph(string userCode, string api)
        {
            HttpResponseMessage response;
            try
            {
                response = _httpClient.GetAsync($"http://127.0.0.1:6002/getRecent?usercode={userCode}&api={api}").Result;
            }
            catch (Exception e) { return (e.Message, null); }

            if (!response.IsSuccessStatusCode)
            {
                return ("服务器内部错误.", null);
            }

            var result = JsonConvert.DeserializeObject<ImgResult>(response.Content.ReadAsStringAsync().Result);
            if (result == null || result.code != 0)
            {
                return (result?.message ?? "数据转换失败.", null);
            }

            byte[] bytes = Convert.FromBase64String(result.data.img);
            return ("", bytes);
        }

        private void OnUnbind(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var reply = "";
            var info = userInfos.FirstOrDefault(x => x.QQUin == messageEvent.MemberUin);
            if (info == null)
            {
                reply = "错误: 当前QQ号不存在绑定的记录.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            else
            {
                userInfos.Remove(info);
                SaveUserInfo();
                reply = $"U{messageEvent.MemberUin} => ∅    解绑成功.\n";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
        }

        private void OnBind(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var reply = "";
            if (args.Count == 0)
            {
                reply = "错误: 缺少参数: <Usercode>.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            var info = userInfos.FirstOrDefault(x => x.QQUin == messageEvent.MemberUin);
            if (info != null)
            {
                reply = "错误: 当前QQ号已存在一个绑定的记录.\n" +
                    "如需更换绑定, 请先使用 /arc unbind 解绑.\n" +
                    $"U{info.QQUin} => {info.Usercode}.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            var usercode = args[0];

            var regex = new Regex(@"[0-9]{9}");
            var match = regex.Match(usercode);

            if (match.Success)
            {
                info = new ArcaeaUserInfo(messageEvent.MemberUin, usercode);
                userInfos.Add(info);
                SaveUserInfo();
                reply = $"U{messageEvent.MemberUin} => {usercode}   绑定成功.\n" +
                    $"现在可以直接使用 /arc b30 来获取成绩图.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
            else
            {
                reply = $"错误: 参数非法: \"{usercode}\" => <Usercode>.";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }
        }
    }
    class ImgResult
    {
        public int code;
        public string message;
        public Data data;
        public class Data
        {
            public string img;
        }
    }

    class SongResult
    {
        public int code;
        public Data data;
        public class Data
        {
            public string song_name;
            public string song_cover;
            public string artist;
            public List<Info> info;
            public class Info
            {
                public int difficulty;
                public int note;
                public int rating;
            }
        }
    }

    class ArcaeaUserInfo
    {
        public uint QQUin;
        public string Usercode;

        public ArcaeaUserInfo(uint qqUin, string usercode)
        {
            QQUin = qqUin;
            Usercode = usercode;
        }
    }
}
