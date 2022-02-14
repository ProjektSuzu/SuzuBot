using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message.Model;
using ProjektRin.Attributes;
using ProjektRin.Commands;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ProjektRin
{
    public class CommandManager
    {
        private static CommandManager _instance = new();
        private CommandManager() { }
        public static CommandManager Instance => _instance;

        private Dictionary<(CommandSet, BaseCommand), List<(Command handler, MethodInfo method)>> _cmdSets = new();
        public Dictionary<(CommandSet, BaseCommand), List<(Command handler, MethodInfo method)>> CmdSets => _cmdSets;
        private static CommandLineInterface _cli = CommandLineInterface.Instance;
        private static BotManager _botManager = BotManager.Instance;
        private static GroupManager _groupManager = GroupManager.Instance;
        private static string TAG = "CMDMGR";

        public void LoadCommands()
        {
            var types = typeof(CommandSet).Assembly.GetTypes();
            foreach (var type in types)
            {
                var attr = type?.GetCustomAttribute<CommandSet>();
                if (attr != null)
                {
                    _cli.Info(TAG, $"Loading Command Set: {attr.Name}.");
                    var table = new List<(Command h, MethodInfo m)>();

                    var instance = (BaseCommand)Activator.CreateInstance(type)!;
                    foreach (var method in type.GetMethods())
                    {
                        foreach (var command in method.GetCustomAttributes())
                        {
                            if (command is GroupMessageCommand cmd)
                            {
                                table.Add((cmd, method));
                                _cli.Info(TAG, $"Loading Command: {cmd.Name}.");
                            }
                        }
                    }
                    
                    instance.OnInit();
                    _cmdSets.Add((attr, instance), table);
                }
            }
            var count = 0;
            foreach (var i in _cmdSets)
            {
                count += i.Value.Count;
            }
            _cli.Info(TAG, $"{_cmdSets.Count} command set(s) found, {count} command(s) loaded.");
        }

        public ((CommandSet, BaseCommand), (Command handler, MethodInfo method))? TryGetCommand(string cmdName)
        {
            foreach (var set in _cmdSets)
            {
                foreach (var cmd in set.Value)
                {
                    if (cmd.handler.Name == cmdName)
                    {
                        var attr = set.Key.Item1;
                        var baseCmd = set.Key.Item2;
                        return new ((attr, baseCmd), (cmd.handler, cmd.method));
                    }
                }
            }

            return null;
        }

        public void ReloadCommands()
        {
            _cmdSets.Clear();
            GC.Collect();
            LoadCommands();
        }

        public int GetCommandSetCount()
            => _cmdSets.Count;

        public int GetCommandCount()
        {
            var count = 0;
            foreach (var i in _cmdSets)
            {
                count += i.Value.Count;
            }
            return count;
        }

        public void GroupMessageEventListener(object? sender, GroupMessageEvent messageEvent)
        {
            var bot = (Bot)sender!;
            if (messageEvent.MemberUin == bot.Uin) return;
            var textChain = messageEvent.Message.GetChain<PlainTextChain>()?.Content.Trim() ?? null;
            if (textChain == null) return;
            var atChain = messageEvent.Message.GetChain<AtChain>();

            if (_groupManager.IsPassiveMode(messageEvent.GroupUin))
            {
                if ((atChain == null || atChain.AtUin != bot.Uin) && (textChain == null || !textChain.StartsWith("铃酱")))
                    return;
                textChain = textChain.Replace("铃酱", "");
                if (atChain != null) textChain = textChain.Replace(atChain.ToString(), "");
                if (!textChain.StartsWith('/')) textChain = '/' + textChain;
            }
            else
            {
                if (atChain != null && atChain.AtUin == bot.Uin || textChain != null && textChain.StartsWith("铃酱"))
                {
                    textChain = textChain.Replace("铃酱", "");
                    if (atChain != null) textChain = textChain.Replace(atChain.ToString(), "");
                    if (!textChain.StartsWith('/')) textChain = '/' + textChain;
                }
            }

            foreach (var set in _cmdSets)
            {
                foreach (var (attr, method) in set.Value)
                {
                    var regexs = attr.Patterns;
                    if (regexs == null)
                    {
                        _ = method.Invoke(bot, new object[] { bot, messageEvent });
                        continue;
                    }
                    foreach (var regex in regexs)
                    {
                        if ((regex.Match(textChain).Success))
                        {
                            if (messageEvent.MemberUin == bot.Uin) return;
                            _ = method.Invoke(set.Key.Item2, new object[] { bot, messageEvent });
                            if (method.ReturnType != typeof(void)) return;

                            _cli.Info(TAG, $"{method.Name} Invoked.");
                        }
                    }
                }
            }
        }
    }
}
