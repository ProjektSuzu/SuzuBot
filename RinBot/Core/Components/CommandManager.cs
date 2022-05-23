using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using NLog;
using RinBot.Commands;
using RinBot.Core.Attributes.Command;
using RinBot.Core.Attributes.Command.Modules;
using RinBot.Core.Attributes.CommandSet;
using RinBot.Utils.Database.Tables;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RinBot.Core.Components
{
    class LoadedCommandSet
    {
        public CommandSet CommandSetAttr;
        public BaseCommand CommandSetClass;
        public bool IsEnabled;

        public List<KeyValuePair<Command, MethodInfo>> Commands;
    }

    internal class CommandManager
    {
        #region 单例模式
        private static CommandManager instance;
        private CommandManager() { }
        public static CommandManager Instance
        {
            get
            {
                if (instance == null) instance = new CommandManager();
                return instance;
            }
        }
        #endregion

        private static readonly string TAG = "CMDMGR";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        private List<LoadedCommandSet> commandSets = new();
        public List<LoadedCommandSet> CommandSets => commandSets;

        private int commandSetCount = 0;
        private int commandCount = 0;

        public int CommandSetCount => commandSetCount;
        public int CommandCount => commandCount;

        public void LoadCommandSets()
        {
            //从当前运行时程序集获取所有类
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                //判断是否继承 BaseCommand
                if (type.BaseType != typeof(BaseCommand)) continue;

                //判断是否拥有 CommandSet 标签
                CommandSet? cmdSetAttr = type.GetCustomAttribute<CommandSet>();
                if (cmdSetAttr == null) continue;

                Logger.Info($"Loading Command Set: {cmdSetAttr.Name}({cmdSetAttr.PackageName})");

                //实例化命令集
                BaseCommand? instance = (BaseCommand?)Activator.CreateInstance(type);
                if (instance == null)
                {
                    Logger.Error($"Unable to load Command Set: {cmdSetAttr.Name}({cmdSetAttr.PackageName}). Skipping.");
                    continue;
                }

                //初始化结构体
                LoadedCommandSet loadedCommandSet = new();
                {
                    loadedCommandSet.CommandSetAttr = cmdSetAttr;
                    loadedCommandSet.CommandSetClass = instance;
                    loadedCommandSet.IsEnabled = true;
                    loadedCommandSet.Commands = new();
                };

                //获取所有方法
                foreach (var method in type.GetMethods())
                {
                    foreach (var methodAttr in method.GetCustomAttributes())
                    {
                        if (methodAttr is Command cmd)
                        {
                            Logger.Info($"> Loading Command: {cmd.Name}");
                            loadedCommandSet.Commands.Add(new(cmd, method));
                        }
                    }
                }

                instance.OnInit();
                instance.OnEnable();

                commandSets.Add(loadedCommandSet);
            }
            commandSetCount = commandSets.Count;
            commandSets.ForEach(x => commandCount += x.Commands.Count);
            Logger.Info($"{CommandSetCount} command set(s) found, {CommandCount} command(s) loaded.");
        }
        public void ReloadCommandSets()
        {
            Logger.Info("Reloading Command Sets.");
            commandSets.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            LoadCommandSets();
        }
        public bool IsCommandSetEnabled(string packageName)
        {
            foreach (var commandSet in commandSets)
            {
                if (commandSet.CommandSetAttr.PackageName == packageName)
                {
                    return commandSet.IsEnabled;
                }
            }
            return false;
        }
        private bool HasPrefix(Bot bot, GroupMessageEvent groupMessageEvent)
        {
            MessageChain messageChain = groupMessageEvent.Message.Chain;
            string? message = messageChain.ToString().Trim();
            AtChain? atChain = (AtChain?)messageChain.FirstOrDefault(x => x is AtChain);
            if (
                !GroupPreferenceManager.Instance.GetPreference(groupMessageEvent.GroupUin).SilentMode && message.StartsWith('/') ||
                message.StartsWith("铃酱") ||
                atChain != null && atChain.AtUin == bot.Uin
                )
                return true;
            return false;
        }
        private string RemovePrefix(Bot bot, MessageChain messageChain)
        {
            string? message = messageChain.ToString().Trim();
            AtChain? atChain = (AtChain?)messageChain.FirstOrDefault(x => x is AtChain && (x as AtChain).AtUin == bot.Uin);
            Regex? regex = new Regex($"(/|铃酱|\\[KQ:at,qq={bot.Uin}\\])([\\s\\S]*)");
            Match? match = regex.Match(message);
            message = match.Groups[2].Value;
            return message.Trim();
        }
        public void OnGroupMessageEvent(Bot bot, GroupMessageEvent groupMessageEvent)
        {
            //排除自己发送的消息
            if (groupMessageEvent.MemberUin == bot.Uin) return;

            //排除被ban的成员
            if (UserInfoManager.GetUserInfo(groupMessageEvent.MemberUin).isBanned) return;

            foreach (var commandSet in commandSets)
            {
                foreach (var command in commandSet.Commands)
                {
                    var messageString = groupMessageEvent.Message.Chain.ToString().Trim();
                    var cmd = command.Key;
                    var method = command.Value;

                    if (cmd is not GroupMessageCommand) continue;

                    if (!(cmd as MessageCommand).IsRaw && !HasPrefix(bot, groupMessageEvent)) continue;
                    messageString = RemovePrefix(bot, groupMessageEvent.Message.Chain);

                    bool? isIntercepted = null;
                    foreach (var regex in (cmd as GroupMessageCommand).Regexs)
                    {
                        bool isMatched = false;
                        if (regex.IsMatch(messageString))
                        {
                            isMatched = true;

                            if (!IsCommandSetEnabled(commandSet.CommandSetAttr.PackageName))
                            {
                                Logger.Warn($"\n{groupMessageEvent.GroupName}({groupMessageEvent.GroupUin})|{groupMessageEvent.MemberCard}({groupMessageEvent.MemberUin}) 尝试调用 {commandSet.CommandSetAttr.Name}:{cmd.Name} 但已被群规则关闭");
                                bot.SendGroupMessage(
                                    groupMessageEvent.GroupUin,
                                    new MessageBuilder()
                                    .Add(ReplyChain.Create(groupMessageEvent.Message))
                                    .Text(
                                        $"命令集 {commandSet.CommandSetAttr.Name} 已被开发者暂时关闭\n" +
                                        $"请联系开发者获取更多信息"
                                    )
                                    );
                                return;
                            }

                            if (!GroupPreferenceManager.Instance.IsCommandSetEnabled(groupMessageEvent.GroupUin, commandSet.CommandSetAttr.PackageName))
                            {
                                Logger.Warn($"\n{groupMessageEvent.GroupName}({groupMessageEvent.GroupUin})|{groupMessageEvent.MemberCard}({groupMessageEvent.MemberUin}) 尝试调用 {commandSet.CommandSetAttr.Name}:{cmd.Name} 但已被全局规则关闭");
                                bot.SendGroupMessage(
                                    groupMessageEvent.GroupUin,
                                    new MessageBuilder()
                                    .Add(ReplyChain.Create(groupMessageEvent.Message))
                                    .Text(
                                        $"命令集 {commandSet.CommandSetAttr.Name} 已被你所在群的管理员禁用\n" +
                                        $"请联系管理员开启此功能"
                                    )
                                    );
                                return;
                            }

                            if (PermissionManager.Instance.GetPermission(bot, groupMessageEvent.GroupUin, groupMessageEvent.MemberUin) < cmd.Permission)
                            {
                                Logger.Warn($"\n{groupMessageEvent.GroupName}({groupMessageEvent.GroupUin})|{groupMessageEvent.MemberCard}({groupMessageEvent.MemberUin}) 尝试调用 {commandSet.CommandSetAttr.Name}:{cmd.Name} 但权限不足");
                                bot.SendGroupMessage(
                                    groupMessageEvent.GroupUin,
                                    new MessageBuilder()
                                    .Add(ReplyChain.Create(groupMessageEvent.Message))
                                    .Text(
                                        $"你没有权限使用此命令\n" +
                                        $"此命令需要的权限等级为 {cmd.Permission}\n" +
                                        $"你的权限等级为 {PermissionManager.Instance.GetPermission(bot, groupMessageEvent.GroupUin, groupMessageEvent.MemberUin)}"
                                    )
                                    );
                                return;
                            }

                            List<string>? args = regex.Match(messageString).Groups.Values.Select(x => x.Value).Skip(1).ToList().FirstOrDefault(defaultValue: "").Split(' ').ToList();
                            args.RemoveAll(x => x.Trim() == "");
                            if (args.All(x => x == ""))
                            {
                                args = new List<string>();
                            }

                            try
                            {
                                switch (method.GetParameters().Count())
                                {
                                    case 3:
                                        {
                                            isIntercepted = (bool?)method.Invoke(commandSet.CommandSetClass, new object[] { bot, groupMessageEvent, args });
                                            break;
                                        }

                                    case 2:
                                        {
                                            isIntercepted = (bool?)method.Invoke(commandSet.CommandSetClass, new object[] { bot, groupMessageEvent });
                                            break;
                                        }

                                    default:
                                        break;
                                }
                                Logger.Info($"{groupMessageEvent.GroupName}({groupMessageEvent.GroupUin})|{groupMessageEvent.MemberCard}({groupMessageEvent.MemberUin}): {cmd.Name} Invoked");
                            }
                            catch (Exception e)
                            {
                                Logger.Error($"{groupMessageEvent.GroupName}({groupMessageEvent.GroupUin})|{groupMessageEvent.MemberCard}({groupMessageEvent.MemberUin}): {cmd.Name} throwed an exception:\n{e.ToString()}");
                            }
                            break;
                        }
                        if (isMatched) break;
                    }
                    if (isIntercepted != null && isIntercepted == true) break;
                }
            }
        }
        public void OnGroupPokeEvent(Bot bot, GroupPokeEvent groupPokeEvent)
        {
            //排除自己发送的消息
            if (groupPokeEvent.OperatorUin == bot.Uin) return;

            //排除被ban的成员
            if (UserInfoManager.GetUserInfo(groupPokeEvent.OperatorUin).isBanned) return;

            foreach (var commandSet in commandSets)
            {
                foreach (var command in commandSet.Commands)
                {
                    var cmd = command.Key;
                    var method = command.Value;

                    if (cmd is not GroupPokeCommand) continue;

                    method.Invoke(commandSet.CommandSetClass, new object[] { bot, groupPokeEvent });
                }
            }

        }
    }
}
