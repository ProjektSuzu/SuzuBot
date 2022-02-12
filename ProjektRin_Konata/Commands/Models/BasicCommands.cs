using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using ProjektRin.Attributes;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ProjektRin.Commands.Models
{
    [CommandSet("BasicCommandSet")]
    internal class BasicCommands : BaseCommand
    {
        public override void OnInit()
        {
        }

        [GroupMessageCommand("help", "查看某条命令的帮助", @"/help")]
        public void OnHelpCmd(Bot bot, GroupMessageEvent messageEvent)
        {
            var textChain = messageEvent.Message.GetChain<PlainTextChain>();
            var regex = new Regex(@"(?<=/help).*");
            var targetCmd = regex.Match(textChain.Content).Value.Trim();
            var message = new MessageBuilder();

            if (targetCmd == "")
            {
                var allCommands = "[Help]\n";
                foreach (var i in CommandManager.Instance.CmdSets.OrderBy(pair => pair.Key.Item1.Name))
                {
                    var cmdSet = i.Key;
                    var cmds = i.Value;
                    allCommands += $"[{cmdSet.Item1.Name}]\n";
                    foreach (var j in cmds.OrderBy(cmd => cmd.handler.Name))
                    {
                        allCommands += $"{j.handler.Name}\n    {j.handler.Description}\n";
                    }
                    allCommands += "\n";
                }

                message = message.PlainText(allCommands);
                _ = bot.SendGroupMessage(messageEvent.GroupUin, message);
                return;
            }

            var result = CommandManager.Instance.TryGetCommand(targetCmd);

            if (result == null)
            {
                message = message.PlainText($"未能找到命令: {targetCmd}");
                _ = bot.SendGroupMessage(messageEvent.GroupUin, message);
            }

            var set = result.Value.Item1.Item1;
            var handler = result.Value.Item2.handler;
            var method = result.Value.Item2.method;

            var reply = $"[Help]\n" +
                $"{handler.Name}\n" +
                $"{handler.Description}\n" +
                $"用法: \n{handler.Pattern}\n\n" +
                
                $"所属命令集: {set.Name}";

            message = message.PlainText(reply);
            _ = bot.SendGroupMessage(messageEvent.GroupUin, message);
        }

        [GroupMessageCommand("echo", "输出一段指定的消息", @"/echo\s([\s\S]+)")]
        public void OnEchoCmd(Bot bot, GroupMessageEvent messageEvent)
        {
            var regex = new Regex(@"(?<=/echo).*");
            var echo = regex.Match(messageEvent.Message.ToString()).Value.Trim();

            var message = MessageBuilder.Eval(echo);

            _ = bot.SendGroupMessage(messageEvent.GroupUin, message);
        }

        [GroupMessageCommand("status", "查看Bot当前的运行状态", @"/status")]
        public void OnStatusCmd(Bot bot, GroupMessageEvent messageEvent)
        {
            var cmdmgr = CommandManager.Instance;

            var osVersion = Environment.OSVersion.Platform;
            var clrVersion = Environment.Version.ToString();
            var processorCount = Environment.ProcessorCount;
            var currentDateTime = DateTime.Now;
            var tickCount = DateTime.Now - Process.GetCurrentProcess().StartTime;
            var usedMemoryMB = Process.GetCurrentProcess().PagedMemorySize64 / 1024 / 1024;

            var groupCount = bot.GetGroupList().Result.Count;
            var friendCount = bot.GetFriendList().Result.Count;

            var reply = $"[ProjektRin] 运行状态汇报\n" +
                $"UTC {currentDateTime.ToUniversalTime()}\n\n" +

                $"当前系统平台: {osVersion} {processorCount} Thread(s)\n" +
                $"CLR版本: {clrVersion}\n" +
                $"内存占用: {usedMemoryMB} MB\n" +
                $"运行时间: {tickCount:dd\\.hh\\:mm\\:ss}\n\n" +

                $"[CMDMGR]\n" +
                $"载入了 {cmdmgr.GetCommandSetCount()} 个命令集, {cmdmgr.GetCommandCount()} 条命令.\n\n" +

                $"[KonataCore]\n" +
                $"共有 {friendCount} 个好友, {groupCount} 个群.";

            var message = new MessageBuilder(reply);

            _ = bot.SendGroupMessage(messageEvent.GroupUin, message);
        }

        [GroupMessageCommand("ping", "检查Bot网络连通情况", @"/ping")]
        public void OnPing(Bot bot, GroupMessageEvent messageEvent)
        {
            var ticksNow = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
            long ticksSend = (long)messageEvent.MessageTime * 1000;

            var reply = $"Pong! ({ticksNow - ticksSend}ms)";

            var message = new MessageBuilder(reply);

            _ = bot.SendGroupMessage(messageEvent.GroupUin, message);
        }

        [GroupMessageCommand("reload", "重新加载所有命令", @"/reload")]
        public void OnReload(Bot bot, GroupMessageEvent messageEvent)
        {
            string reply;
            MessageBuilder message;
            try
            {
                var cmdmgr = CommandManager.Instance;
                cmdmgr.ReloadCommands();
            }
            catch(Exception e)
            {
                reply = $"重新载入失败: {e.Message}";
                message = new MessageBuilder(reply);
                return;
            }
            reply = "载入成功";
            message = new MessageBuilder(reply);
            _ = bot.SendGroupMessage(messageEvent.GroupUin, message);
        }
    }
}
