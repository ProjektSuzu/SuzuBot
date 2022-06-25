using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json;
using RinBot.Core.Attributes.Command.Modules;
using RinBot.Core.Attributes.CommandSet;
using RinBot.Core.Components;
using RinBot.Utils;
using RinBot.Utils.Database.Tables;

namespace RinBot.Commands.Modules.Adventure
{
    [CommandSet("探险", "com.akulak.adventure", false)]
    internal class Adventure : BaseCommand
    {
        private readonly string advStatesPath = Path.Combine(BotManager.configPath, "advStates.json");
        private Dictionary<uint, AdvState> advStates;

        public override void OnInit()
        {
            Load();
        }

        public void Save()
        {
            File.WriteAllText(advStatesPath, JsonConvert.SerializeObject(advStates));
        }

        public void Load()
        {
            if (File.Exists(advStatesPath))
            {
                advStates = JsonConvert.DeserializeObject<Dictionary<uint, AdvState>>(File.ReadAllText(advStatesPath));
            }
            else
            {
                advStates = new();
                Save();
            }
        }

        [GroupMessageCommand("探险", new[] { @"^adventure$", @"^探险$" })]
        public void OnAdventure(Bot bot, GroupMessageEvent messageEvent)
        {
            var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);
            AdvState? advState;
            //存在进行中的事件链或冷却
            if (advStates.ContainsKey(messageEvent.MemberUin))
            {
                advState = advStates[messageEvent.MemberUin];
                //还在冷却
                if (advState.CoolDown > DateTime.Now)
                {
                    var timeDelta = advState.CoolDown - DateTime.Now;
                    messageEvent.Reply(bot, new MessageBuilder().Add(ReplyChain.Create(messageEvent.Message))
                        .Text($"探险进度冷却中\n下一次探险还需要 {((int)timeDelta.TotalMinutes > 0 ? $"{(int)timeDelta.TotalMinutes} 分钟" : $"{(int)timeDelta.TotalSeconds} 秒")}.")); ;
                    return;
                }
            }
            else
            {
                advState = new AdvState()
                {
                    CoolDown = DateTime.Now,
                    Content = "",
                    NextEventID = ""
                };
            }

            if (advState.NextEventID == "")
            {
                advState = AdventureManager.StartNewEvent(ref info);
                var timeDelta = advState.CoolDown - DateTime.Now;
                messageEvent.Reply(bot, new MessageBuilder().Add(ReplyChain.Create(messageEvent.Message))
                        .Text(advState.Content.Replace("{name}", messageEvent.MemberCard))
                        .Text($"\n\n下一次探险还需要 {((int)timeDelta.TotalMinutes > 0 ? $"{(int)timeDelta.TotalMinutes} 分钟" : $"{(int)timeDelta.TotalSeconds} 秒")}."));
            }
            else
            {
                advState = AdventureManager.ExecEvent(advState.NextEventID, ref info);
                var timeDelta = advState.CoolDown - DateTime.Now;
                messageEvent.Reply(bot, new MessageBuilder().Add(ReplyChain.Create(messageEvent.Message))
                        .Text(advState.Content.Replace("{name}", messageEvent.MemberCard))
                        .Text($"\n\n下一次探险还需要 {((int)timeDelta.TotalMinutes > 0 ? $"{(int)timeDelta.TotalMinutes} 分钟" : $"{(int)timeDelta.TotalSeconds} 秒")}."));
            }
            advStates.Remove(messageEvent.MemberUin);
            advStates.Add(messageEvent.MemberUin, advState);
            UserInfoManager.UpdateUserInfo(info);
            Save();
        }
#if DEBUG
        [GroupMessageCommand("探险冷却重置", new[] { @"^adventure-reset" })]
        public void OnAdventureReset(Bot bot, GroupMessageEvent messageEvent)
        {
            if (advStates.ContainsKey(messageEvent.MemberUin))
            {
                advStates[messageEvent.MemberUin].CoolDown = DateTime.Now;
            }
        }

        [GroupMessageCommand("探险触发", new[] { @"^adventure-event\s?([\s\S]+)?" })]
        public void OnAdventureEvent(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            if (args.Count == 0)
                return;

            Console.WriteLine(args[0]);
            var info = UserInfoManager.GetUserInfo(messageEvent.MemberUin);
            AdvState? advState;

            advState = AdventureManager.ExecEvent(args[0], ref info);
            var timeDelta = advState.CoolDown - DateTime.Now;
            messageEvent.Reply(bot, new MessageBuilder().Add(ReplyChain.Create(messageEvent.Message))
                    .Text(advState.Content.Replace("{name}", messageEvent.MemberCard))
                    .Text($"\n\n下一次探险还需要 {((int)timeDelta.TotalMinutes > 0 ? $"{(int)timeDelta.TotalMinutes} 分钟" : $"{(int)timeDelta.TotalSeconds} 秒")}."));

            advStates.Remove(messageEvent.MemberUin);
            advStates.Add(messageEvent.MemberUin, advState);
            UserInfoManager.UpdateUserInfo(info);
            Save();
        }
#endif
    }
}
