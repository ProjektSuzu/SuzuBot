using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using NLog;
using ProjektRin.Core.Attributes.Command.Modules;
using ProjektRin.Core.Attributes.CommandSet;
using ProjektRin.Core.Components;
using ProjektRin.Utils;
using ProjektRin.Utils.BuildStamp;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProjektRin.Commands.Modules
{
    [CommandSet("核心功能", "com.akulak.core")]
    internal class Core : BaseCommand
    {
        private GroupPreferenceManager groupPreferenceManager;
        private CommandManager commandManager;
        private PermissionManager permissionManager;

        private static readonly string TAG = "CORECMD";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);
        public string Introduction =>
            $"[自我介绍]\n" +
            $"这里是铃, 是基于 .NET 下的 Konata 框架\n" +
            $"由 AkulaKirov 开发和维护的机器人\n" +
            $"如果想要使用铃, 请使用 \"/\" \"铃酱\" 或者 At 的方式, 并加上想要调用的命令即可\n" +
            $"目前铃还有很多需要完善的地方, 如果有好的建议可以加入 RinBot 认领群(955578812)进行讨论\n" +
            $"或者使用 /反馈 加上想要反馈的内容就可以了\n" +
            $"期待今后和你们共度的时光\n" +
            $"☆ ～('▽^人)";

        public string Announcement =
            "目前所有使用帮助已经移动到网页服务\n" +
            "请访问 https://docs-rinbot.akulak.icu 来获取帮助信息";

        public override void OnInit()
        {
            groupPreferenceManager = GroupPreferenceManager.Instance;
            commandManager = CommandManager.Instance;
            permissionManager = PermissionManager.Instance;
        }

        [GroupMessageCommand("帮助", new[] { @"^help", @"^帮助" })]
        public void OnHelp(Bot bot, GroupMessageEvent messageEvent)
        {
            MultiMsgChain multiReply = MultiMsgChain.Create();
            multiReply.AddMessage(new MessageStruct(bot.Uin, bot.Name, new MessageBuilder(Introduction).Build()));
            multiReply.AddMessage(new MessageStruct(bot.Uin, bot.Name, new MessageBuilder(Announcement).Build()));
            messageEvent.Reply(bot, new MessageBuilder(multiReply));
        }

        [GroupMessageCommand("反馈", new[] { @"^feedback\s?([\s\S]+)?", @"^反馈\s?([\s\S]+)?" })]
        public void OnFeedback(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            if (args.Count == 0)
            {
                messageEvent.Reply(bot, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text("忘写反馈内容啦o(*≧д≦)o!!")
                    );
                return;
            }

            string rawMessage = messageEvent.Message.Chain.ToString();
            rawMessage = rawMessage.Split("feedback", 2).Last();
            rawMessage = rawMessage.Split("反馈", 2).Last();
            rawMessage = "[用户反馈]\n" + rawMessage.Trim();

            bot.SendGroupMessage(BotManager.DevGroupUin, MessageBuilder.Eval(rawMessage));
            messageEvent.Reply(bot, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text("已收到你的反馈(≧ω≦)/"));
        }

        [GroupMessageCommand("公告", new[] { @"^announcement\s?([\s\S]+)?", @"^公告\s?([\s\S]+)?" }, Permission.Admin)]
        public void OnAnnouncement(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            if (args.Count == 0)
            {
                messageEvent.Reply(bot, new MessageBuilder()
                    .Add(ReplyChain.Create(messageEvent.Message))
                    .Text("忘写公告内容啦o(*≧д≦)o!!")
                    );
                return;
            }

            string rawMessage = messageEvent.Message.Chain.ToString();
            rawMessage = rawMessage.Split("announcement", 2).Last();
            rawMessage = rawMessage.Split("公告", 2).Last();
            rawMessage = "[开发者公告]\n" + rawMessage.Trim();

            var groupList = bot.GetGroupList(true).Result;
            int count = 0;
            for (; count < groupList.Count; count++)
            {
                var uin = groupList[count].Uin;
                bot.SendGroupMessage(uin, MessageBuilder.Eval(rawMessage + $"\n\n{count + 1}/{groupList.Count}"));
            }

            messageEvent.Reply(bot, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text($"已通知了 {count} 个群"));
        }

        [GroupMessageCommand("命令集热重载", new[] { @"^reload", @"^重载" }, Permission.Admin)]
        public void OnReload(Bot bot, GroupMessageEvent messageEvent)
        {
            commandManager.ReloadCommandSets();
            messageEvent.Reply(bot, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text("所有命令集重载成功"));
        }

        [GroupMessageCommand("状态汇报", regex: @"^status")]
        public void OnStatus(Bot bot, GroupMessageEvent messageEvent)
        {
            int processorCount = Environment.ProcessorCount;
            long usedMemoryMB = Environment.WorkingSet / 1024 / 1024;
            TimeSpan tickCount = DateTime.Now - Process.GetCurrentProcess().StartTime;

            int groupCount = bot.GetGroupList(true).Result.Count;
            int friendCount = bot.GetFriendList(true).Result.Count;

            string report =
                $"[RinBot] {RinBuildStamp.Version}\n" +
                $"{RinBuildStamp.Branch}@{RinBuildStamp.CommitHash}\n" +
                $"当前系统平台: {RuntimeInformation.RuntimeIdentifier} {processorCount} Thread(s)\n" +
                $"DotNET 版本: {RuntimeInformation.FrameworkDescription}\n" +
                $"内存占用: {usedMemoryMB} MB\n" +
                $"运行时间: {tickCount:dd\\d\\ hh\\h\\ mm\\m\\ ss\\s}\n\n" +

                $"[CMDMGR]\n" +
                $"载入了 {commandManager.CommandSetCount} 个命令集, {commandManager.CommandCount} 条命令.\n\n" +

                $"[KonataCore] {CoreBuildStamp.Version}\n" +
                $"{CoreBuildStamp.Branch}@{CoreBuildStamp.CommitHash}\n" +
                $"共有 {friendCount} 个好友, {groupCount} 个群.\n\n" +

                $"{DateTime.Now:O}\nEOT";

            messageEvent.Reply(bot, new MessageBuilder(report));
        }

        [GroupMessageCommand("Ping", @"^ping")]
        public void OnPing(Bot bot, GroupMessageEvent messageEvent)
        {
            string reply = $"Pong!";
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text("Pong!"));
        }
    }
}
