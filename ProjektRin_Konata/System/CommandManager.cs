using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using NLog;
using ProjektRin.Attributes.Command;
using ProjektRin.Attributes.CommandSet;
using ProjektRin.Commands;
using ProjektRin.Utils.Database.Tables;
using System.Reflection;
using static ProjektRin.System.PermissionManager;

namespace ProjektRin.System
{
    internal class CommandManager
    {
        private static CommandManager _instance = new();
        private CommandManager() { }
        public static CommandManager Instance => _instance;

        private static BotManager _botManager = BotManager.Instance;
        private static GroupManager _groupManager = GroupManager.Instance;
        private static readonly string TAG = "CMDMGR";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        private Dictionary<(CommandSet, BaseCommand), List<(Command handler, MethodInfo method)>> _cmdSets = new();
        public Dictionary<(CommandSet, BaseCommand), List<(Command handler, MethodInfo method)>> CmdSets => _cmdSets;

        public int CommandSetCount
            => _cmdSets.Count;

        public int CommandCount
        {
            get
            {
                var count = 0;
                foreach (var i in _cmdSets)
                {
                    count += i.Value.Count;
                }
                return count;
            }
        }


        public void LoadCommandSet()
        {
            //获取所有加载的类
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                //判断是否为 BaseCommand 类
                if (type.BaseType != typeof(BaseCommand)) continue;
                var attr = type.GetCustomAttribute<CommandSet>();
                //判断是否有 CommandSet 特性
                if (attr == null) continue;

                Logger.Info($"Loading Command Set: {attr.Name}.");
                var instance = (BaseCommand)Activator.CreateInstance(type)!;
                var commands = new List<(Command handler, MethodInfo method)>();

                foreach (var method in type.GetMethods())
                {
                    foreach (var command in method.GetCustomAttributes())
                    {
                        //通过属性获取方法
                        if (command is Command cmd)
                        {
                            commands.Add((cmd, method));
                            Logger.Info($"Loading Command: {cmd.Name}");
                        }
                    }
                }

                //执行启动前方法
                instance.OnInit();
                instance.OnEnable();

                _cmdSets.Add((attr, instance), commands);
            }
            Logger.Info($"{CommandSetCount} command set(s) found, {CommandCount} command(s) loaded.");
        }

        public void ReloadCommandSet()
        {
            foreach (var cmdSet in _cmdSets)
                cmdSet.Key.Item2.OnDisable();
            _cmdSets.Clear();
            GC.Collect();
            LoadCommandSet();
        }

        public bool HasCommandSet(string name)
        {
            return _cmdSets.Any(x => x.Key.Item1.Name == name);
        }

        public bool ToggleCommandSet(string name, bool action)
        {
            if (!HasCommandSet(name))
            {
                throw new InvalidOperationException($"命令集 \"{name}\" 不存在.");
            }

            if (name == "CoreCommands")
            {
                throw new InvalidOperationException($"命令集 \"{name}\" 是核心部件.");
            }

            var set = _cmdSets.First(x => x.Key.Item1.Name == name).Key.Item2;
            if (action)
                set.OnEnable();
            else
                set.OnDisable();
            return true;
        }

        public void GroupPokeEventHandler(object sender, GroupPokeEvent groupPokeEvent)
        {
            Bot bot = (Bot)sender;
            //排除自己发送的戳一戳
            if (groupPokeEvent.OperatorUin == bot.Uin) return;
            //黑名单
            var info = UserInfoManager.GetUserInfo(groupPokeEvent.OperatorUin);
            if (info != null && info.isBanned)
            {
                Logger.Info($"U{groupPokeEvent.OperatorUin} is Banned.");
                return;
            }

            foreach (var set in _cmdSets)
            {
                if (!set.Key.Item2.IsEnabled || _groupManager.IsCommandSetDisabled(groupPokeEvent.GroupUin, set.Key.Item1.PackageName)) continue;
                foreach (var (attr, method) in set.Value)
                {
                    if (attr is GroupPokeCommand)
                    {
                        //获取用户权限组
                        var permission = Permission.User;
                        if (PermissionManager.Instance.IsAdmin(groupPokeEvent.OperatorUin))
                        {
                            permission = Permission.Root;
                        }
                        else if (PermissionManager.Instance.IsOperator(groupPokeEvent.GroupUin, groupPokeEvent.OperatorUin))
                        {
                            permission = Permission.Operator;
                        }

                        if (permission < attr.Permission)
                        {
                            Logger.Warn($"G{groupPokeEvent.GroupUin}|U{groupPokeEvent.OperatorUin} => {method.Name} Rejected.");
                            var reply = $"你没有足够的权限来执行这条命令: {attr.Name}\n要求 {attr.Permission}.";
                            bot.SendGroupMessage(groupPokeEvent.GroupUin, new MessageBuilder(reply));
                            return;
                        }

                        var methodReturn = (bool?)method.Invoke(set.Key.Item2, new object[] { bot, groupPokeEvent }) ?? true;
                        return;
                    }
                }

            }
        }

        public void GroupMessageEventHandler(object sender, GroupMessageEvent groupMessageEvent)
        {
            Bot bot = (Bot)sender;
            //排除自己发送的消息
            if (groupMessageEvent.MemberUin == bot.Uin) return;
            //黑名单
            var info = UserInfoManager.GetUserInfo(groupMessageEvent.MemberUin);
            if (info != null && info.isBanned)
            {
                Logger.Info($"U{groupMessageEvent.MemberUin} is Banned.");
                return;
            }
            var message = groupMessageEvent.Message.ToString().Trim();
            if (message == null) return;
            if (!ShouldProcess(bot, groupMessageEvent)) return;

            message = RemoveCommandIndicator(bot, groupMessageEvent.Message);

            foreach (var set in _cmdSets)
            {
                foreach (var (attr, method) in set.Value)
                {
                    if (attr is GroupMessageCommand)
                    {
                        var patterns = attr.Patterns;

                        foreach (var pattern in patterns)
                        {
                            if (pattern.Match(message).Success)
                            {
                                if (!set.Key.Item2.IsEnabled || _groupManager.IsCommandSetDisabled(groupMessageEvent.GroupUin, set.Key.Item1.PackageName))
                                {
                                    Logger.Warn($"G{groupMessageEvent.GroupUin}|U{groupMessageEvent.GroupUin} => {set.Key.Item1.Name} Rejected.");
                                    var reply = $"命令集被禁用: {set.Key.Item1.Name}\n请联系管理员或开发者来开启该功能.";
                                    bot.SendGroupMessage(groupMessageEvent.GroupUin, new MessageBuilder(reply));
                                    continue;
                                }
                                //获取用户权限组
                                var permission = Permission.User;
                                if (groupMessageEvent.MemberUin == 1785416538u)
                                {
                                    permission = Permission.Root;
                                }
                                else if (PermissionManager.Instance.IsOperator(groupMessageEvent))
                                {
                                    permission = Permission.Operator;
                                }

                                if (permission < attr.Permission)
                                {
                                    Logger.Warn($"G{groupMessageEvent.GroupUin}|U{groupMessageEvent.MemberUin} => {method.Name} Rejected.");
                                    var reply = $"你没有足够的权限来执行这条命令: {attr.Name}\n要求 {attr.Permission}.";
                                    bot.SendGroupMessage(groupMessageEvent.GroupUin, new MessageBuilder(reply));
                                    return;
                                }

                                bool methodReturn = true;
                                if (method.GetParameters().Count() == 2)
                                    methodReturn = (bool?)method.Invoke(set.Key.Item2, new object[] { bot, groupMessageEvent }) ?? true;
                                else if (method.GetParameters().Count() == 3)
                                {
                                    var args = pattern.Match(message).Groups.Values.Select(x => x.Value).Skip(1).ToList().FirstOrDefault(defaultValue: "").Split(' ').ToList();
                                    args.RemoveAll(x => x.Trim() == "");
                                    if (args.All(x => x == "")) args = new List<string>();
                                    methodReturn = (bool?)method.Invoke(set.Key.Item2, new object[] { bot, groupMessageEvent, args }) ?? true;
                                }
                                else
                                    continue;

                                Logger.Info($"G{groupMessageEvent.GroupUin}|U{groupMessageEvent.MemberUin} => {method.Name} Invoked.");

                                if (methodReturn)
                                    return;
                                else
                                    continue;
                            }
                        }
                    }
                }
            }

        }

        private bool ShouldProcess(Bot bot, GroupMessageEvent groupMessageEvent)
        {
            var messageChain = groupMessageEvent.Message;
            var message = messageChain.ToString().Trim();
            var atChain = (AtChain?)messageChain.FirstOrDefault(x => x is AtChain);
            if (
                !_groupManager.IsPassiveMode(groupMessageEvent.GroupUin) && message.StartsWith('/') ||
                message.StartsWith("铃酱") ||
                atChain != null && atChain.AtUin == bot.Uin
                ) return true;

            return false;
        }

        private string RemoveCommandIndicator(Bot bot, MessageChain messageChain)
        {
            var message = messageChain.ToString().Trim();
            var atChain = (AtChain?)messageChain.FirstOrDefault(x => x is AtChain && (x as AtChain).AtUin == bot.Uin);

            message = message.Replace("铃酱", "");
            if (atChain != null) message = message.Replace(atChain.ToString(), "");
            message = message.Split('/', 2).Last();

            return message.Trim();
        }

    }
}
