using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using NLog;
using RinBot.Core.Attributes.Command.Modules;
using RinBot.Core.Attributes.CommandSet;
using RinBot.Core.Components;
using RinBot.Utils;
using RinBot.Utils.BuildStamp;
using RinBot.Utils.Database.Tables;
using SkiaSharp;
using SkiaSharp.QrCode;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RinBot.Commands.Modules
{
    [CommandSet("核心功能", "com.akulak.core")]
    internal class Core : BaseCommand
    {
        private GroupPreferenceManager groupPreferenceManager;
        private CommandManager commandManager;
        private PermissionManager permissionManager;

        private static readonly string TAG = "CORECMD";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public string Announcement =
            $"[RinBot] {RinBuildStamp.Version}\n" + 
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
            messageEvent.Reply(bot, new MessageBuilder(Announcement));
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
                $"共有 {friendCount} 个好友, {groupCount} 个群.\n" +
                $"自动同意: {(BotManager.AutoAccept ? "开启" : "关闭")}.\n\n" +


                $"{DateTime.Now:O}\nEOT";

            messageEvent.Reply(bot, new MessageBuilder(report));
        }

        [GroupMessageCommand("Ping", @"^ping")]
        public void OnPing(Bot bot, GroupMessageEvent messageEvent)
        {
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text("Pong!"));
        }

        [GroupMessageCommand("GarbageCollect", @"^gc")]
        public void OnGarbageCollect(Bot bot, GroupMessageEvent messageEvent)
        {
            var workingSetBefore = Environment.WorkingSet;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var workingSetAfter = Environment.WorkingSet;
            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text($"Collected. ({workingSetBefore / 1024 / 1024} MB -> {workingSetAfter / 1024 / 1024} MB)"));
        }

        [GroupMessageCommand("静默模式", @"^silent\s?([\s\S]+)?")]
        public void OnSilentMode(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var groupUin = messageEvent.GroupUin;

            if (args.Count > 0)
            {
                var targetUin = args.First();
                if (uint.TryParse(targetUin, out groupUin))
                {
                    if (permissionManager.GetPermission(bot, messageEvent.GroupUin, messageEvent.MemberUin) < Permission.Admin)
                    {
                        messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text(
                                $"你没有权限使用此参数: <groupUin>\n" +
                                $"此命令需要的权限等级为 {Permission.Admin}\n" +
                                $"你的权限等级为 {PermissionManager.Instance.GetPermission(bot, messageEvent.GroupUin, messageEvent.MemberUin)}"));
                        return;
                    }
                }
                else
                {
                    messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"错误: 参数非法: \"{targetUin}\" => groupUin"));
                    return;
                }
            }

            var preference = groupPreferenceManager.GetPreference(groupUin);
            preference.SilentMode = !preference.SilentMode;
            groupPreferenceManager.Save();
            messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"G{groupUin} => 已{(preference.SilentMode ? "开启" : "关闭")}静默模式"));
            return;
        }

        [GroupMessageCommand("命令集控制", @"^cmdctl\s?([\s\S]+)?", Permission.Operator)]
        public void OnCommandSetControl(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            if (args.Count == 0)
            {
                string reply = $"本群的命令集规则:\n";
                foreach (var loadedCommandSet in commandManager.CommandSets)
                {
                    bool globallyStatus = loadedCommandSet.IsEnabled;
                    bool locallyStatus = groupPreferenceManager.IsCommandSetEnabled(messageEvent.GroupUin, loadedCommandSet.CommandSetAttr.PackageName);

                    reply += $"{(globallyStatus ? "◆" : "◇")}|{(locallyStatus ? "●" : "○")} {loadedCommandSet.CommandSetAttr.Name}\n";
                }
                bool silent = groupPreferenceManager.GetPreference(messageEvent.GroupUin).SilentMode;

                reply +=
                    $"\n静默模式: {(silent ? "开启" : "关闭")} \n\n" +
                    "◆/◇ 为全局开关状态\n" +
                    "●/○ 为本群开关状态\n";

                messageEvent.Reply(bot, new MessageBuilder(reply));
                return;
            }
            else
            {
                uint groupUin = messageEvent.GroupUin;
                bool action;
                bool global = false;
                List<string> commandSets = new();
                List<string> packageNames = new();

                var actionStr = args.First();
                args.RemoveAt(0);
                if (actionStr == "enable")
                    action = true;
                else if (actionStr == "disable")
                    action = false;
                else
                {
                    messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"错误: 参数非法: \"{actionStr}\" => enable/disable"));
                    return;
                }

                while (args.Count > 0)
                {
                    var item = args.First();
                    args.RemoveAt(0);

                    switch (item)
                    {
                        case "-g":
                            {
                                if (permissionManager.GetPermission(bot, messageEvent.GroupUin, messageEvent.MemberUin) < Permission.Admin)
                                {
                                    messageEvent.Reply(bot, new MessageBuilder()
                                        .Add(ReplyChain.Create(messageEvent.Message))
                                        .Text(
                                            $"你没有权限使用此参数: -g\n" +
                                            $"此命令需要的权限等级为 {Permission.Admin}\n" +
                                            $"你的权限等级为 {PermissionManager.Instance.GetPermission(bot, messageEvent.GroupUin, messageEvent.MemberUin)}"));
                                    return;
                                }
                                global = true;
                                break;
                            }

                        case "-t":
                            {
                                if (args.Count == 0)
                                {
                                    messageEvent.Reply(bot, new MessageBuilder()
                                        .Add(ReplyChain.Create(messageEvent.Message))
                                        .Text($"错误: 缺少参数: targetGroupUin"));
                                    return;
                                }

                                var targetGroupUin = args.First();
                                args.RemoveAt(0);

                                if (uint.TryParse(targetGroupUin, out groupUin))
                                {
                                    if (permissionManager.GetPermission(bot, messageEvent.GroupUin, messageEvent.MemberUin) < Permission.Admin)
                                    {
                                        messageEvent.Reply(bot, new MessageBuilder()
                                            .Add(ReplyChain.Create(messageEvent.Message))
                                            .Text(
                                                $"你没有权限使用此参数: -t <targetGroupUin>\n" +
                                                $"此命令需要的权限等级为 {Permission.Admin}\n" +
                                                $"你的权限等级为 {PermissionManager.Instance.GetPermission(bot, messageEvent.GroupUin, messageEvent.MemberUin)}"));
                                        return;
                                    }
                                }
                                else
                                {
                                    messageEvent.Reply(bot, new MessageBuilder()
                                        .Add(ReplyChain.Create(messageEvent.Message))
                                        .Text($"错误: 参数非法: \"{targetGroupUin}\" => <targetGroupUin>"));
                                    return;
                                }
                                break;
                            }

                        default:
                            {
                                commandSets.Add(item);
                                break;
                            }
                    }
                }

                foreach (var x in commandSets)
                {
                    if (x == "核心功能")
                    {
                        messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"错误: 核心功能 无法操作"));
                        return;
                    }
                    var commandSet = commandManager.CommandSets.FirstOrDefault(y => y.CommandSetAttr.Name == x);
                    if (commandSet == null)
                    {
                        messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"错误: 参数非法: \"{x}\" => <cmdSets>"));
                        return;
                    }
                    else
                    {
                        packageNames.Add(commandSet.CommandSetAttr.PackageName);
                    }
                }

                if (global)
                {
                    foreach (var x in packageNames)
                    {
                        commandManager.CommandSets.First(y => y.CommandSetAttr.PackageName == x).IsEnabled = action;
                    }
                    Logger.Info($"已全局{(action ? "开启" : "关闭")} {packageNames.Count()} 个命令集");
                    messageEvent.Reply(bot, new MessageBuilder()
                                .Add(ReplyChain.Create(messageEvent.Message))
                                .Text($"已全局{(action ? "开启" : "关闭")} {packageNames.Count()} 个命令集"));
                    return;
                }
                else
                {
                    var preference = groupPreferenceManager.GetPreference(groupUin);
                    foreach (var x in packageNames)
                    {
                        if (preference.CommandSetPreferences.ContainsKey(x))
                        {
                            preference.CommandSetPreferences[x] = action;
                        }
                        else
                        {
                            preference.CommandSetPreferences.Add(x, action);
                        }
                    }
                    groupPreferenceManager.Save();
                    Logger.Info($"G{groupUin} => 已{(action ? "开启" : "关闭")} {packageNames.Count()} 个命令集");
                    messageEvent.Reply(bot, new MessageBuilder()
                                .Add(ReplyChain.Create(messageEvent.Message))
                                .Text($"G{groupUin} => 已{(action ? "开启" : "关闭")} {packageNames.Count()} 个命令集"));
                    return;
                }
            }
        }

        [GroupMessageCommand("用户信息", new[] { @"^info\s?([\s\S]+)?", @"^信息\s?([\s\S]+)?" })]
        public void OnInfo(Bot bot, GroupMessageEvent messageEvent, List<string> args)
        {
            var reply = "";
            uint uin = messageEvent.MemberUin;

            if (args.Count > 0)
            {
                if(!uint.TryParse(args.First(), out uin))
                {
                    var chain = MessageBuilder.Eval(args.First()).Build().GetChain<AtChain>();
                    if (chain == null)
                    {
                        reply = $"错误: 未指定的对象.";
                        bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                        return;
                    }
                    else
                    {
                        uin = chain.AtUin;
                    }
                }
            }

            UserInfo? info = UserInfoManager.GetUserInfo(uin);
            //应该不会 但是以防万一
            if (info == null)
            {
                reply = $"错误: 找不到用户: \"U{uin}\".";
                bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder(reply));
                return;
            }

            reply =
                $"[UserInfo]用户信息" + "\n" +
                $"用户名: {info.uin}" + "\n" +
                $"内存: {UserInfoManager.CoinToString(info.coin)}" + "\n" +
                $"等级: {info.level}" + "\n" +
                $"经验: {info.exp} exp\n" +
                $"距离下一等级还需经验: {UserInfoManager.LevelToExp(info.level) - info.exp} exp";

            bot.SendGroupMessage(messageEvent.GroupUin, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Text(reply));
        }

        [GroupMessageCommand("自动同意请求", new[] { @"^auto-accept", @"^自动同意" }, Permission.Admin)]
        public void OnAutoAccept(Bot bot, GroupMessageEvent messageEvent)
        {
            BotManager.AutoAccept = !BotManager.AutoAccept;
            messageEvent.Reply(bot, new MessageBuilder()
                            .Add(ReplyChain.Create(messageEvent.Message))
                            .Text($"自动接受已{(BotManager.AutoAccept ? "开启" : "关闭")}"));
            return;
        }

#if DEBUG
        [GroupMessageCommand("Test", new[] { @"^test" }, Permission.Admin)]
        public void OnTest(Bot bot, GroupMessageEvent messageEvent)
        {
            var image = ImageChain.Create(File.ReadAllBytes(Path.Combine(BotManager.resourcePath, "test.png")));
            bot.UploadGroupImage(image, messageEvent.GroupUin).Wait();
            byte[]? bytes = null;
            using (var generator = new QRCodeGenerator())
            {
                var qr = generator.CreateQrCode(image.ImageUrl, ECCLevel.M);
                var info = new SKImageInfo(512, 512);
                using (var surface = SKSurface.Create(info))
                {
                    var canvas = surface.Canvas;
                    canvas.Render(qr, info.Width, info.Height);

                    using (var qrImage = surface.Snapshot())
                    using (var data = qrImage.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        bytes = data.ToArray();
                    }
                }
            }
            messageEvent.Reply(bot, new MessageBuilder()
                .Add(ReplyChain.Create(messageEvent.Message))
                .Image(bytes));
            return;
        }
#endif
    }
}
