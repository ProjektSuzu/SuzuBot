using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using NLog;
using ProjektRin.Attributes;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjektRin.Commands.Models
{
    [CommandSet("ArcaeaCommandSet")]
    internal class ArcaeaCommands : BaseCommand
    {
        private string rootPath;
        private string resourcePath;
        private string pythonPath;
        private Process python;

        private static HttpClient _httpClient;

        private bool localStatus;
        private bool remoteStatus;

        private Dictionary<uint, string> _playerInfo;

        private static string TAG = "Arcaea";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public override void OnInit()
        {
            _httpClient = new HttpClient();

            rootPath = AppDomain.CurrentDomain.BaseDirectory;
            resourcePath = Path.Combine(rootPath, "resources");
            pythonPath = Path.Combine(resourcePath, "ArcaeaProbe_Rework");
            python = new Process();

            try
            {
                LoadPlayerInfo();
            } catch { }
            finally { SavePlayerInfo(); }

            python.StartInfo.UseShellExecute = false;
            python.StartInfo.CreateNoWindow = true;
            python.StartInfo.FileName = "python3";
            python.StartInfo.Arguments = Path.Combine(pythonPath, "main.py");

            AppDomain.CurrentDomain.ProcessExit += (s, e) => python.Kill();
            python.Start();
            Logger.Info("Python daemon started.");

            CheckServer();
        }

        private void CheckServer()
        {
            HttpResponseMessage response = new();
            localStatus = false;
            try
            {
                response = _httpClient.GetAsync("http://127.0.0.1:6002").Result;
                localStatus = response.IsSuccessStatusCode;
            }
            catch { remoteStatus = localStatus = false; return; }

            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content.ReadAsStringAsync().Result);
                remoteStatus = (int)(dict?["status"]) == 0;
            } catch { remoteStatus = localStatus = false; return; }
            
        }

        private void LoadPlayerInfo()
        {
            var json = File.ReadAllText(Path.Combine(resourcePath, "arcaea.json"));
            _playerInfo = JsonConvert.DeserializeObject<Dictionary<uint, string>>(json) ?? new Dictionary<uint, string>();
        }

        private void SavePlayerInfo()
        {
            var json = JsonConvert.SerializeObject(_playerInfo);
            File.WriteAllText(Path.Combine(resourcePath, "arcaea.json"), json, Encoding.UTF8);
        }

        private (string, byte[]?) GetB30Graph(string userCode)
        {
            HttpResponseMessage response;
            try
            {
                response = _httpClient.GetAsync($"http://127.0.0.1:6002/getB30?usercode={userCode}").Result;
            }
            catch(Exception e) { return (e.Message, null); }
            if (!response.IsSuccessStatusCode)
            {
                return ("服务器内部错误.", null);
            }
            var result = JsonConvert.DeserializeObject<B30Result>(response.Content.ReadAsStringAsync().Result);
            if (result == null || result.code != 0)
            {
                return (result?.message ?? "数据转换失败.", null);
            }

            byte[] bytes = Convert.FromBase64String(result.data.img);
            return ("", bytes);

        }

        [GroupMessageCommand("arc",
            "调用Arcaea相关的功能\n" +
            "输入 /arc 查看更多帮助",
            "/arc <功能名> [<参数>]",
            @"^arc\s?([\S]+)?\s?([\s\S]+)?")]
        public void OnArcaea(Bot bot, GroupMessageEvent messageEvent)
        {
            var textChain = messageEvent.Message.GetChain<PlainTextChain>();
            var regex = new Regex(@"arc\s?([\S]+)?\s?([\s\S]+)?");
            var match = regex.Match(textChain.Content.ToString()).Groups.Values.Skip(1).Select(v => v.Value);
            var funcName = match.First().Trim();
            var args = match.Last().Trim().Split(' ').ToList();
            CheckServer();
            if (funcName != null && funcName != "")
            {
                if (funcName == "b30")
                {
                    CheckServer();

                    OnArcaeaB30(bot, messageEvent, args);
                    return;
                }
                else if (funcName == "bind")
                {
                    OnArcaeaBind(bot, messageEvent, args);
                    return;
                }
                else if (funcName == "unbind")
                {
                    OnArcaeaUnbind(bot, messageEvent);
                    return;
                }
                else
                {
                    _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder($"未能识别的功能: {funcName}"));
                    return;
                }
            }

            var reply = $"[Arcaea]\n" +
                $"b30\n用法: arc b30 [<好友代码>]\n    查看B30成绩图\n" +
                $"bind\n用法: arc bind <好友代码>\n    为当前QQ号绑定一个好友代码\n" +
                $"unbind\n用法: arc unbind\n    为当前QQ号解除绑定好友代码\n" +
                $"\n" +
                $"本地服务器连通性: {(localStatus ? "OK" : "FAIL")}\n" +
                $"远端服务器连通性: {(remoteStatus ? "OK" : "FAIL")}";

            var message = new MessageBuilder(reply);
            _ = bot.SendGroupMessage(messageEvent.GroupUin, message);
        }

        public void OnArcaeaUnbind(Bot bot, GroupMessageEvent messageEvent)
        {
            if (_playerInfo.ContainsKey(messageEvent.MemberUin))
            {
                _playerInfo.Remove(messageEvent.MemberUin);
                SavePlayerInfo();   
                _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("绑定记录已清除"));
            }
            else
            {
                _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("你当前没有任何绑定记录"));
            }
        }

        public void OnArcaeaBind(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var userCode = "";

            if (_playerInfo.TryGetValue(messageEvent.MemberUin, out userCode))
            {
                _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("当前用户已存在绑定记录\n" +
                    $"{userCode} => U{messageEvent.MemberUin}"));
                return;
            }
            userCode = args.FirstOrDefault(defaultValue: "");
            var regex = new Regex(@"[0-9]{9}");
            var match = regex.Match(userCode);

            if (match.Success)
            {
                _playerInfo.Add(messageEvent.MemberUin, match.Value);
                SavePlayerInfo();
                _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("绑定成功\n" +
                    $"{match.Value} => U{messageEvent.MemberUin}"));
            }
            else
            {
                _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("无法识别的好友代码\n" +
                    "请检查输入格式"));
            }
        }

        public void OnArcaeaB30(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            if (!localStatus || !remoteStatus)
            {
                _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("本地服务器或远端服务器无法连接"));
                return;
            }
            var userCode = args.FirstOrDefault(defaultValue: "");
            var regex = new Regex(@"[0-9]{9}");
            var match = regex.Match(messageEvent.Message.ToString());

            if (match.Success)
            {
                userCode = match.Value.Trim();
            }
            else
            {
                userCode = _playerInfo.GetValueOrDefault(messageEvent.MemberUin, "");
            }

            if (userCode == "")
            {
                _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("没有找到你的记录\n" +
                    "请确认已经绑定好友码或使用参数调用"));
                return;
            }

            _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .At(messageEvent.MemberUin)
                .PlainText("收到, 正在处理成绩图...")
                );

            var (message, bytes) = GetB30Graph(userCode);
            if (message != "")
            {
                _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("发生错误: \n" +
                    message));
                return;
            }

            var reply = new MessageBuilder()
                .At(messageEvent.MemberUin)
                .Image(bytes);


            _ = bot.SendGroupMessage(messageEvent.GroupUin, reply);
        }
    }

    class B30Result
    {
        public int code;
        public string message;
        public Data data;
        public class Data
        {
            public string img;
        }
    }
}