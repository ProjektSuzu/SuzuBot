using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using RinBot.Core.Attributes.Command.Modules;
using RinBot.Core.Attributes.CommandSet;
using RinBot.Core.Components;
using RinBot.Utils;
using RinBot.Utils.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RinBot.Commands.Modules.Apex.ApexMapRotation.Mode;

namespace RinBot.Commands.Adventure
{
    //虽然内存是写在UserInfoManager里的
    //但是懒 有空再搬过来

    [CommandSet("探险", "com.akulak.adventure")]
    internal class Adventure : BaseCommand
    {
        private List<AdvEvent> advEvents;

        public override void OnInit()
        {
            string? json = File.ReadAllText(BotManager.resourcePath + "/adventureEvent.json");
            advEvents = JsonConvert.DeserializeObject<List<AdvEvent>>(json) ?? new();
        }

        [GroupMessageCommand("探险冷却重置", new[] { @"^adventure-reset" }, Permission.Admin)]
        public void OnAdventureReset(Bot bot, GroupMessageEvent messageEvent)
        {
            var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);
            info.nextAdventure = DateTime.Now;
            UserInfoManager.UpdateUserInfo(info);
            messageEvent.Reply(bot, new MessageBuilder().Add(ReplyChain.Create(messageEvent.Message))
                    .Text("Cooldown Reset."));
            return;
        }

        [GroupMessageCommand("探险", new[] { @"^adventure$", @"^探险" })]
        public void OnAdventure(Bot bot, GroupMessageEvent messageEvent)
        {
            var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);
            if (info.nextAdventure > DateTime.Now)
            {
                messageEvent.Reply(bot, new MessageBuilder().Add(ReplyChain.Create(messageEvent.Message))
                    .Text($"你还需要等待 {(int)(info.nextAdventure - DateTime.Now).TotalMinutes} 分钟才能进行下一次探险"));
                return;
            }

            info.nextAdventure = DateTime.Now.AddMinutes(30);
            var advEvent = advEvents[new Random().Next(advEvents.Count)];

            StringBuilder reply = new();
            reply.AppendLine(advEvent.Content.Replace("{name}", messageEvent.MemberCard) + "\n");

            var effects = advEvent.Effect.Split(';');
            foreach (var effectStr in effects)
            {
                var effect = effectStr.Split(':');
                var valueRange = effect[1].Split('?');
                int value = 0;
                if (valueRange.Count() == 1)
                {
                    value = int.Parse(valueRange[0]);
                }
                else
                {
                    int min = int.Parse(valueRange[0]);
                    int max = int.Parse(valueRange[1]);
                    value = new Random().Next(min, max);
                }
                switch (effect[0])
                {
                    case "coin":
                        {
                            if (value < 0)
                            {
                                if (info.coin + value < 0)
                                    value = (int)-info.coin;
                                info.coin -= (uint)Math.Abs(value);
                            }
                            else
                            {
                                info.coin += (uint)value;
                            }
                            reply.AppendLine($"内存{(value > 0 ? "增加了" : "减少了")} {(UserInfoManager.CoinToString((uint)Math.Abs(value)))}");
                            break;
                        }

                    case "exp":
                        {
                            info.exp += value;
                            if (info.exp >= UserInfoManager.LevelToExp(info.level))
                            {
                                info.exp -= UserInfoManager.LevelToExp(info.level);
                                info.level++;
                            }
                            reply.AppendLine($"经验{(value > 0 ? "增加了" : "减少了")} {(uint)Math.Abs(value)} exp");
                            break;
                        }

                    case "fav":
                        {
                            info.favorability += value;
                            reply.AppendLine($"好感度{(value > 0 ? "增加了" : "减少了")} {(uint)Math.Abs(value)}");
                            break;
                        }
                    default:break;
                }
            }
            UserInfoManager.UpdateUserInfo(info);
            messageEvent.Reply(bot, new MessageBuilder().Add(ReplyChain.Create(messageEvent.Message))
                    .Text(reply.ToString()));
            return;
        }
    }

    internal class AdvEvent
    {
        [JsonProperty("content")]
        public string Content;
        [JsonProperty("effect")]
        public string Effect;
    }
}
