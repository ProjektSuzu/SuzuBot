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
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                if (type.BaseType != typeof(BaseCommand)) continue;
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
            var message = messageEvent.Message.ToString().Trim() ?? null;
            if (message == null) return;
            var atChain = messageEvent.Message.GetChain<AtChain>();

            if (_groupManager.IsPassiveMode(messageEvent.GroupUin))
            {
                if ((atChain == null || atChain.AtUin != bot.Uin) && (message == null || !message.StartsWith("铃酱")))
                    return;
                message = message.Replace("铃酱", "");
                if (atChain != null) message = message.Replace(atChain.ToString(), "");
            }
            else
            {
                if ((atChain == null || atChain.AtUin != bot.Uin)
                    && (message == null || !message.StartsWith("铃酱"))
                    && (message == null || !message.StartsWith('/'))
                    )
                {
                    return;
                }
                message = message.Replace("铃酱", "");
                if (atChain != null) message = message.Replace(atChain.ToString(), "");
                if (message.StartsWith('/')) message = message.Replace("/", "");
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
                        if ((regex.Match(message).Success))
                        {
                            if (messageEvent.MemberUin == bot.Uin) return;
                            object methodReturn;
                            if (method.GetParameters().Count() == 2)
                                methodReturn = method.Invoke(set.Key.Item2, new object[] { bot, messageEvent }) ?? true;
                            else if (method.GetParameters().Count() == 3)
                                methodReturn = method.Invoke(set.Key.Item2, new object[] { bot, messageEvent, regex.Match(message).Groups.Values.Select(x => x.Value).Skip(1).ToList() }) ?? true;
                            else
                                continue;

                            _cli.Info(TAG, $"{method.Name} Invoked.");
                            if (method.ReturnType != typeof(bool))
                            {
                                if ((bool)methodReturn)
                                    return;
                            }
                            else return;

                        }
                    }
                }
            }
        }
    }
}
