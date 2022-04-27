using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.Components;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("一眼丁真", "com.akulak.dingzhen")]
    internal class DingzhenCommand : BaseCommand
    {
        private DirectoryInfo picDir;
        public override string Help => 
            $"[一眼丁真]\n" +
            $"/dingzhen [-a <pic>]      发丁真图\n" +
            $"\n" +
            $"  -a <pic>     添加图片\n" +
            $"\n" +
            $"快捷名:\n" +
            $"/丁真\n" +
            $"\n" +
            $"  pic     图片";

        public override void OnInit()
        {
            picDir = new DirectoryInfo(Path.Combine(BotManager.resourcePath, "Dingzhen-Oneeye"));
        }

        [GroupMessageCommand("发丁真图片", new[] { @"^dingzhen\s?([\s\S]+)?", @"^丁真\s?([\s\S]+)?" })]
        public void OnSendLoongPic(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            string? reply = "";
            if (args.Count == 0)
            {
                Random random = new Random();
                FileInfo? pic = picDir.GetFiles()[random.Next(picDir.GetFiles().Length)];
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                                                        .Image(pic.FullName));
                return;
            }

            string? arg = "";
            while (args.Count > 0)
            {
                arg = args[0];
                args.RemoveAt(0);

                switch (arg)
                {
                    case "-a":
                    case "添加":
                        {
                            IEnumerable<BaseChain>? chain = messageEvent.Message.Chain & BaseChain.ChainType.Image;
                            if (chain == null || chain.Count() == 0)
                            {
                                reply = $"错误: 未找到图片";
                                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                                return;
                            }
                            int count = 0;
                            HttpClient client = new HttpClient();
                            foreach (ImageChain img in chain)
                            {
                                try
                                {
                                    string? url = $"https://gchat.qpic.cn/gchatpic_new/0/0-0-{img.FileHash}/0";
                                    string? name = img.FileName;
                                    byte[]? data = client.GetAsync(url).Result.Content.ReadAsByteArrayAsync().Result;
                                    File.WriteAllBytesAsync(Path.Combine(picDir.FullName, $"{name}.png"), data);
                                    count++;
                                }
                                catch
                                {
                                    continue;
                                }
                            }

                            reply = $"成功添加了 {count} 张图片";
                            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                            return;
                        }

                    default:
                        {
                            reply = $"错误: 未知参数: \"{arg}\"";
                            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                            return;
                        }
                }
            }

        }

    }
}
