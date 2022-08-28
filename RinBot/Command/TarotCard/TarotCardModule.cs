using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using RinBot.Core;
using RinBot.Core.Components.Attributes;
using RinBot.Core.Components.Commands;
using RinBot.Core.KonataCore.Events;

namespace RinBot.Command.TarotCard
{
    [Module("塔罗牌", "AkulaKirov.TarotCard")]
    internal class TarotCardModule
    {
        public static readonly string RESOURCE_DIR_PATH = Path.Combine(GlobalScope.RESOURCE_DIR_PATH, "AkulaKirov.TarotCard");

        public TarotCardModule()
        {
            
        }

        // 没有十年脑血栓写不出这些文本
        private string[] tarotDrawText = new[]
        {
            "{name} 的回合，Draw!",
            "正在为 {name} 洗牌中",
            "全能的塔罗牌啊，请为 {name} 指点迷津吧！",
        };

        [TextCommand("获取塔罗牌", new[] { "tarot", "塔罗牌" })]
        public void OnGetTarot(MessageEventArgs messageEvent, CommandStruct command)
        {
            var messageBuilder = new MessageBuilder();
            int num = 1;
            if (command.FuncArgs.Length != 0)
            {
                if (!int.TryParse(command.FuncArgs[0], out num) || num < 0 || num > 6)
                {
                    messageBuilder.Text($"[TarotCard]\n参数非法: {command.FuncArgs[0]} => [<num>]");
                    messageEvent.Reply(messageBuilder);
                    return;
                }
            }
            var multiMsg = new MultiMsgChain();
            int counter = 0;
            multiMsg.Add(new MessageStruct(GlobalScope.KonataBot.Bot.Uin, GlobalScope.KonataBot.Bot.Name, new MessageBuilder("[TarotCard]\n" +
                tarotDrawText[new Random().Next(tarotDrawText.Length)].Replace(
                    "{name}",
                    messageEvent.Sender.Name)).Build()));
            foreach (var tarot in TarotCards.GetTarotCards(num))
            {
                counter++;
                var cardMessageBuilder = new MessageBuilder();

                cardMessageBuilder.Text($"#{counter:00} : {tarot.Name} {(tarot.IsReversed ? "逆位" : "正位")}\n");
                cardMessageBuilder.Image(File.ReadAllBytes(tarot.ImagePath));
                cardMessageBuilder.Text($"\n释义: \n{(tarot.IsReversed ? tarot.Info.ReverseDescribe : tarot.Info.Describe)}");

                var messageStruct = new MessageStruct(GlobalScope.KonataBot.Bot.Uin, GlobalScope.KonataBot.Bot.Name, cardMessageBuilder.Build());
                multiMsg.Add(messageStruct);
            }
            messageBuilder.Add(multiMsg);
            messageEvent.Subject.SendMessage(messageBuilder.Build());
            return;
        }

        
    }
}
