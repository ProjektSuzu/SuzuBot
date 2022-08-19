using Konata.Core.Message;
using RinBot.Core.Components.Messages;
using RinBot.Core.Components.Messages.Models;
using KAtChain = Konata.Core.Message.Model.AtChain;
using KImageChain = Konata.Core.Message.Model.ImageChain;
using KMultiMsgChain = Konata.Core.Message.Model.MultiMsgChain;
using KReplyChain = Konata.Core.Message.Model.ReplyChain;
using KTextChain = Konata.Core.Message.Model.TextChain;

namespace RinBot.Core.KonataCore
{
    internal class KonataMessageParser
    {
        #region Singleton
        public static KonataMessageParser Instance = new Lazy<KonataMessageParser>(() => new KonataMessageParser()).Value;
        private KonataMessageParser()
        {

        }
        #endregion

        public RinMessageChain ToRinChain(MessageChain chains)
        {
            RinMessageChain rinMessageChains = new();
            foreach (var chain in chains)
            {
                switch (chain.Type)
                {
                    case Konata.Core.Message.BaseChain.ChainType.Text:
                        {
                            rinMessageChains.Add(TextChain.Create((chain as KTextChain).Content));
                            break;
                        }
                    case Konata.Core.Message.BaseChain.ChainType.Image:
                        {
                            var imgHash = (chain as KImageChain).FileHash;
                            rinMessageChains.Add(TextChain.Create(imgHash));
                            break;
                        }
                    case Konata.Core.Message.BaseChain.ChainType.At:
                        {
                            rinMessageChains.Add(MentionedChain.Create((chain as KAtChain).AtUin.ToString()));
                            break;
                        }
                    case Konata.Core.Message.BaseChain.ChainType.Reply:
                        {
                            // TODO: 缓存10条消息来保留上下文 并以此定位
                            break;
                        }
                    case Konata.Core.Message.BaseChain.ChainType.MultiMsg:
                        {
                            // 类不透明 无法转化
                            break;
                        }
                }
            }
            return rinMessageChains;
        }

        public MessageChain ToKonataChain(RinMessageChain chains)
        {
            MessageBuilder builder = new();
            foreach (var chain in chains)
            {
                switch (chain.Type)
                {
                    case Components.Messages.BaseChain.ChainType.Text:
                        {
                            builder.Text((chain as TextChain).Content);
                            break;
                        }
                    case Components.Messages.BaseChain.ChainType.Image:
                        {
                            builder.Image((chain as ImageChain).Bytes);
                            break;
                        }
                    case Components.Messages.BaseChain.ChainType.Reply:
                        {
                            MessageStruct messageStruct;
                            var messageEvent = (chain as ReplyChain).TargetMessageEvent;
                            if (messageEvent is Konata.Core.Events.Model.GroupMessageEvent groupMsgEvent)
                            {
                                messageStruct = groupMsgEvent.Message;
                            }
                            else if (messageEvent is Konata.Core.Events.Model.FriendMessageEvent friendMsgEvent)
                            {
                                messageStruct = friendMsgEvent.Message;
                            }
                            else
                            {
                                throw new ArgumentException();
                            }

                            builder.Add(KReplyChain.Create(messageStruct));
                            break;
                        }
                    case Components.Messages.BaseChain.ChainType.Mentioned:
                        {
                            builder.At(uint.Parse((chain as MentionedChain).TargetId));
                            break;
                        }
                    case Components.Messages.BaseChain.ChainType.MultiMsg:
                        {
                            KMultiMsgChain multiMsg = KMultiMsgChain.Create();
                            foreach (var msg in (chain as MultiMsgChain).Messages)
                            {
                                var senderId = uint.Parse(msg.SenderId);
                                var senderName = msg.SenderName;
                                var konataMsgChain = ToKonataChain(msg.Chains);
                                multiMsg.Add(new MessageStruct(senderId, senderName, konataMsgChain));
                            }
                            builder.Add(multiMsg);
                            break;
                        }
                }
            }
            return builder.Build();
        }
    }
}
