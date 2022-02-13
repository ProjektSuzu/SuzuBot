using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using ProjektRin.Attributes;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ProjektRin.Commands.Models
{
    [CommandSet("ArcaeaCommandSet")]
    internal class ArcaeaCommands : BaseCommand
    {
        private string rootPath;
        private string resourcePath;
        private Process python;

        private static HttpClient _httpClient;


        private static CommandLineInterface _cli = CommandLineInterface.Instance;
        private static string TAG = "Arcaea";

        public override void OnInit()
        {
            _httpClient = new HttpClient();

            rootPath = Directory.GetCurrentDirectory();
            resourcePath = rootPath + "/resources/ArcaeaProbe_Rework";
            python = new Process();

            python.StartInfo.UseShellExecute = false;
            python.StartInfo.CreateNoWindow = true;
            python.StartInfo.FileName = "python3";
            python.StartInfo.Arguments = $"{resourcePath}/main.py";
            //python.StartInfo.RedirectStandardOutput = true;

            python.OutputDataReceived += (s, e) => _cli.Info(TAG, e.Data);

            AppDomain.CurrentDomain.ProcessExit += (s, e) => python.Kill();

            python.Start();
            _cli.Info(TAG, "Python daemon started.");

        }

        [GroupMessageCommand("arcaea",
            "调用Arcaea相关的功能",
            "/arcaea <功能名> [<参数>]",
            @"/arcaea")]
        public void OnArcaea(Bot bot, GroupMessageEvent messageEvent)
        {
            var textChain = messageEvent.Message.GetChain<PlainTextChain>();
            //var regex = new Regex(@"(?<=/arcaea).*");
            var reply = "";

            HttpResponseMessage result = new();
            bool localStatus = false;
            try
            {
                result = _httpClient.GetAsync("http://127.0.0.1:6002").Result;
                localStatus = result.IsSuccessStatusCode;
            }
            catch { }
            

            if (localStatus)
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Content.ReadAsStringAsync().Result);
                var remoteStatus = (bool)(dict["status"] ?? false);

                reply = $"[Arcaea]\n" +
                    $"本地服务器连通性: {(localStatus ? "OK" : "FAIL")}\n" +
                    $"远端服务器连通性: {(remoteStatus ? "OK" : "FAIL")}";
            }
            else
            {
                reply = "[Arcaea]\n" +
                    "本地服务器连通性: FAIL\n" +
                    "远端服务器连通性: FAIL";
            }

            var message = new MessageBuilder(reply);

            _ = bot.SendGroupMessage(messageEvent.GroupUin, message);
        }

        //[GroupMessageCommand("arcaea b30", "获取指定用户的B30成绩图", @"/arcaea b30")]
        public void OnArcaeaB30(Bot bot, GroupMessageEvent messageEvent)
        {
            var regex = new Regex(@"(?<=/arcaea b30)[0-9]*");
            var match = regex.Match(messageEvent.Message.ToString());
            if (!match.Success)
            {
                _ = bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder("没有找到你的记录\n" +
                    "请确认已经绑定好友码或使用参数调用"));
            }
        }
    }
}
