using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using System.Text.RegularExpressions;
using static Konata.Core.Message.BaseChain;

namespace ProjektRin.Commands.Modules.Github
{
    [CommandSet("Github", "com.akulak.githunFunc")]
    internal class Github : BaseCommand
    {
        public override string Help =>
            "";

        public override void OnInit()
        {
        }

        [GroupMessageCommand("Github图片生成", @"^https://github.com/", isRaw: true)]
        public void OnGithubUrl(Bot bot, GroupMessageEvent messageEvent)
        {
            IEnumerable<BaseChain>? textChain = messageEvent.Message.Chain & ChainType.Text;
            string? url = string.Join("", textChain.Select(x => (x as TextChain).Content));
            HttpClient? client = new HttpClient();
            string? result = client.GetStringAsync(url).Result;
            Regex? regex = new Regex("<meta property=\"og:image\" content=\"([^\"]+)");
            Match? match = regex.Match(result);
            if (match.Success)
            {
                string? imageUrl = match.Groups[1].Value;
                byte[]? img = client.GetByteArrayAsync(imageUrl).Result;
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder().Image(img));
            }
        }
    }
}
