using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.Components;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("龙图", "com.akulak.loongPic")]
    internal class LoongPicCommand : BaseCommand
    {
        private DirectoryInfo picDir;
        public override string Help => $"[龙图]\n" +
                $"/loong [-a <pic>]      发龙图\n" +
                $"\n" +
                $"  -a <pic>     添加图片\n" +
                $"\n" +
                $"快捷名:\n" +
                $"/龙图\n" +
                $"\n" +
                $"  pic     图片";
        public override void OnInit()
        {
            picDir = new DirectoryInfo(Path.Combine(BotManager.resourcePath, "LoongPic"));
        }

        [GroupMessageCommand("发龙图", new[] { @"^loong\s?([\s\S]+)?", @"^龙图\s?([\s\S]+)?" })]
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
                                string? name = img.FileName;
                                byte[]? data = img.FileData;
                                FileStream fs = new FileStream(Path.Combine(picDir.FullName, $"/{name}.png"), FileMode.Create);
                                fs.Write(data, 0, data.Length);
                                fs.Close();
                                count++;
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
