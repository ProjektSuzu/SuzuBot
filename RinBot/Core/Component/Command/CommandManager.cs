using Konata.Core;
using Konata.Core.Events.Model;
using NLog;
using RinBot.Core.Component.Command.CustomAttribute;
using RinBot.Core.Component.Database;
using RinBot.Core.Component.ENV;
using RinBot.Core.Component.Event;
using RinBot.Core.Component.Message;
using RinBot.Core.Component.Message.Model;
using RinBot.Core.Component.Permission;
using SQLite;
using System.Reflection;
using System.Text.RegularExpressions;
using AtChain = Konata.Core.Message.Model.AtChain;

namespace RinBot.Core.Component.Command
{
    [Table("T_MODULE_INFO")]
    class ModuleInfo
    {
        [PrimaryKey]
        [Column("module_id")]
        public string ModuleID { get; set; }
        [Column("module_name")]

        public string ModuleName { get; set; }
        [Column("is_enable")]
        public bool IsEnable { get; set; }
        [Column("default_enable_type")]
        public ModuleEnableConfig DefaultEnableType { get; set; }
    }

    class Module
    {
        public object ModuleClass { get; set; }
        public ModuleAttribute ModuleAttribute { get; set; }
        public bool IsEnable { get; set; }
        public List<Command> Commands { get; set; }
    }

    class Command
    {
        public CommandAttribute Attribute { get; set; }
        public MethodInfo Method { get; set; }
    }

    internal class CommandManager
    {
        #region Singleton
        private static CommandManager instance;
        public static CommandManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new();
                return instance;
            }
        }
        private CommandManager()
        {
            RinDatabase.Instance.dbConnection.CreateTable<ModuleInfo>();
            RinDatabase.Instance.dbConnection.CreateTable<CommandInvokeRecord>();
        }
        #endregion

        readonly Logger Logger = LogManager.GetLogger("CMD");

        const string CUSTOM_CMD_PREFIX = "CUSTOM_CMD_PREFIX";

        private List<string> leadChars = new()
        {
            "/"
        };

        private List<Module> modules = new();

        public int ModuleCount => modules.Count;
        public int CommandCount => modules.Sum(x => x.Commands.Count);

        public List<ModuleInfo> GetModuleInfos()
            => RinDatabase.Instance.dbConnection
            .Table<ModuleInfo>().ToList();

        internal void RegisterCommands()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var moduleAttribute = type.GetCustomAttribute<ModuleAttribute>();
                if (moduleAttribute == null) continue;

                Logger.Info($"Loading Module: {moduleAttribute.ModuleName}({moduleAttribute.ModuleID})");

                Module module = new()
                {
                    ModuleAttribute = moduleAttribute,
                    IsEnable = true,
                    Commands = new(),
                };

                var methods = type.GetMethods();

                foreach (var method in methods)
                {
                    var commandAttributes = method.GetCustomAttributes<CommandAttribute>();
                    if (!commandAttributes.Any()) continue;
                    foreach (var command in commandAttributes)
                    {
                        Logger.Info($"> Loading Command: {command.Name}({method.Name})");
                        module.Commands.Add(new Command() { Attribute = command, Method = method });
                    }
                }

                module.ModuleClass = Activator.CreateInstance(type);
                modules.Add(module);
            }
            int commandCount = modules.Sum(x => x.Commands.Count);
            var moduleIds = modules.Select(x => x.ModuleAttribute.ModuleID).ToList();
            RinDatabase.Instance.dbConnection
                .Table<ModuleInfo>()
                .ToList()
                .ForEach(x => modules.First(y => y.ModuleAttribute.ModuleID == x.ModuleID).IsEnable = x.IsEnable);
            modules.ForEach(x =>
            {
                RinDatabase.Instance.dbConnection
                .InsertOrReplace(new ModuleInfo()
                {
                    ModuleID = x.ModuleAttribute.ModuleID,
                    ModuleName = x.ModuleAttribute.ModuleName,
                    IsEnable = x.IsEnable,
                    DefaultEnableType = x.ModuleAttribute.ModuleEnableConfig,
                });
            });

            Logger.Info($"Total {modules.Count} Module(s), {commandCount} Command(s).");
        }

        internal void ClearCommands()
        {
            modules.Clear();
        }

        public Module? GetModule(string moduleId)
        {
            return modules.FirstOrDefault(x => x.ModuleAttribute.ModuleID == moduleId);
        }

        internal bool LeadCharCheck(RinEvent rinMessageEvent)
        {
            if (rinMessageEvent.EventSourceType == EventSourceType.QQ)
            {
                var bot = (Bot)rinMessageEvent.OriginalSender;
                foreach (var leadChar in leadChars)
                    if (rinMessageEvent.RawContent.StartsWith(leadChar))
                        return true;
                if (EnvManager.Instance.HasEnv(CUSTOM_CMD_PREFIX))
                {
                    foreach (var leadChar in EnvManager.Instance.GetEnv(CUSTOM_CMD_PREFIX))
                    {
                        if (rinMessageEvent.RawContent.StartsWith(leadChar))
                            return true;
                    }
                }

                if (rinMessageEvent.EventSubjectType == EventSubjectType.Group)
                {
                    var atChain = (rinMessageEvent.OriginalEvent as GroupMessageEvent).Message.Chain.FirstOrDefault();
                    if (atChain != null && atChain is AtChain && (atChain as AtChain).AtUin == bot.Uin)
                        return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        internal string DropLeadChar(RinEvent rinMessageEvent)
        {
            if (rinMessageEvent.EventSourceType == EventSourceType.QQ)
            {
                var bot = (Bot)rinMessageEvent.OriginalSender;
                foreach (var leadChar in leadChars)
                    if (rinMessageEvent.RawContent.StartsWith(leadChar))
                        return rinMessageEvent.RawContent.Substring(leadChar.Length).Trim();

                if (EnvManager.Instance.HasEnv(CUSTOM_CMD_PREFIX))
                {
                    foreach (var leadChar in EnvManager.Instance.GetEnv(CUSTOM_CMD_PREFIX))
                    {
                        if (rinMessageEvent.RawContent.StartsWith(leadChar))
                            return rinMessageEvent.RawContent.Substring(leadChar.Length).Trim();
                    }
                }

                if (rinMessageEvent.EventSubjectType == EventSubjectType.Group)
                {
                    var atChain = (rinMessageEvent.OriginalEvent as GroupMessageEvent).Message.Chain.FirstOrDefault();
                    if (atChain != null && atChain is AtChain && (atChain as AtChain).AtUin == bot.Uin)
                        return rinMessageEvent.RawContent.Substring(atChain.ToString().Length).Trim();
                }

                return rinMessageEvent.RawContent;
            }
            else
            {
                return rinMessageEvent.RawContent;
            }
        }

        internal void CommandInvoke(RinEvent rinEvent)
        {
            //BlackListCheck
            if (rinEvent.EventSourceType == EventSourceType.QQ)
            {
                if (rinEvent.EventSubjectType == EventSubjectType.Group)
                {
                    if (PermissionManager.Instance.IsQQGroupBlackListed(uint.Parse(rinEvent.SubjectId)))
                    {
                        Logger.Warn($"Target group in blacklist: G{rinEvent.SubjectId}");
                        return;
                    }
                    if (PermissionManager.Instance.GetQQUserRole(uint.Parse(rinEvent.SenderId)) <= UserRole.Banned)
                    {
                        Logger.Warn($"Target user in blacklist: U{rinEvent.SenderId}");
                        return;
                    }
                }
                else if (rinEvent.EventSubjectType == EventSubjectType.DirectMessage)
                {
                    if (PermissionManager.Instance.GetQQUserRole(uint.Parse(rinEvent.SenderId)) <= UserRole.Banned)
                    {
                        Logger.Warn($"Target user in blacklist: U{rinEvent.SenderId}");
                        return;
                    }
                }
            }

            List<string> disabled = new();
            if (rinEvent.EventSubjectType == EventSubjectType.Group)
            {
                if (rinEvent.EventSourceType == EventSourceType.QQ)
                {
                    var groupId = uint.Parse(rinEvent.SubjectId);
                    var info = PermissionManager.Instance.GetQQGroupInfo(groupId);

                    disabled = info.DisableModuleIds;
                }
            }

            foreach (var module in modules)
            {
                if (!module.IsEnable) continue;
                if (rinEvent.EventSubjectType == EventSubjectType.Group)
                {
                    if (module.ModuleAttribute.ModuleEnableConfig == ModuleEnableConfig.NormallyEnable)
                    {
                        if (disabled.Any(x => x == module.ModuleAttribute.ModuleID))
                            continue;
                    }
                    if (module.ModuleAttribute.ModuleEnableConfig == ModuleEnableConfig.NormallyDisable)
                    {
                        if (disabled.All(x => x != module.ModuleAttribute.ModuleID))
                            continue;
                    }
                }


                foreach (var command in module.Commands)
                {
                    var attr = command.Attribute;
                    var method = command.Method;

                    if ((attr.EventSourceMask & (int)rinEvent.EventSourceType) == 0)
                        continue;

                    bool invoke = false;
                    var content = DropLeadChar(rinEvent);
                    string matchedPattern = "";

                    if ((attr.MatchingMask & (int)MatchingType.Always) > 1)
                    {
                        invoke = method.GetParameters().Count() > 0 && method.GetParameters().First().ParameterType == rinEvent.OriginalEvent.GetType();
                    }
                    else
                    {
                        if ((attr.MatchingMask & (int)MatchingType.NoLeadChar) == 0)
                            if (!LeadCharCheck(rinEvent)) continue;


                        if ((attr.MatchingMask & (int)MatchingType.Exact) >= 1)
                        {
                            foreach (string cmd in attr.Command)
                                if (content == cmd)
                                {
                                    invoke = true;
                                    break;
                                }
                        }

                        if ((attr.MatchingMask & (int)MatchingType.Contains) >= 1)
                        {
                            foreach (string cmd in attr.Command)
                                if (content.Contains(cmd))
                                {
                                    invoke = true;
                                    break;
                                }
                        }

                        if ((attr.MatchingMask & (int)MatchingType.StartsWith) >= 1)
                        {
                            foreach (string cmd in attr.Command)
                                if (content.StartsWith(cmd))
                                {
                                    invoke = true;
                                    break;
                                }
                        }

                        if ((attr.MatchingMask & (int)MatchingType.Regex) >= 1)
                        {
                            foreach (string cmd in attr.Command)
                                if (Regex.IsMatch(content, cmd))
                                {
                                    invoke = true;
                                    matchedPattern = cmd;
                                    break;
                                }
                        }
                    }

                    var invokeRecord = new CommandInvokeRecord()
                    {
                        Module = module.ModuleAttribute.ModuleID,
                        Command = command.Attribute.Name,
                        SourceType = rinEvent.EventSourceType,
                        SenderId = rinEvent.SenderId,
                        SubjectId = rinEvent.SubjectId,
                        MessageContent = rinEvent.RawContent,
                        Date = DateTime.Now,
                        IsInvoked = false,
                    };

                    if (invoke)
                    {
                        if (PermissionManager.Instance.GetQQUserRole(uint.Parse(rinEvent.SenderId)) < attr.Role)
                        {
                            if (rinEvent.EventSubjectType == EventSubjectType.Group)
                            {
                                if (rinEvent.EventSourceType == EventSourceType.QQ
                                    && PermissionManager.Instance.GetQQUserRoleInGroup(uint.Parse(rinEvent.SenderId), uint.Parse(rinEvent.SubjectId)) >= attr.Role)
                                {
                                    // 无事发生
                                }
                                else
                                {
                                    Logger.Warn($"Permission denied: U{rinEvent.SenderId} Require {attr.Role}");
                                    var messageChain = new RinMessageChain();
                                    messageChain.Add(ReplyChain.Create(rinEvent.OriginalEvent));
                                    messageChain.Add(TextChain.Create($"权限不足: 需要 {attr.Role}"));
                                    rinEvent.Reply(messageChain);
                                    return;
                                }
                            }
                            else
                            {
                                Logger.Warn($"Permission denied: U{rinEvent.SenderId} Require {attr.Role}");
                                var messageChain = new RinMessageChain();
                                messageChain.Add(ReplyChain.Create(rinEvent.OriginalEvent));
                                messageChain.Add(TextChain.Create($"权限不足: 需要 {attr.Role}"));
                                rinEvent.Reply(messageChain);
                                return;
                            }
                        }

                        Logger.Info($"Command {attr.Name} Invoked: {rinEvent.SenderId} at {rinEvent.SubjectId}");


                        try
                        {
                            List<string> args = new();
                            if ((attr.MatchingMask & (int)MatchingType.Regex) >= 1)
                            {
                                var match = Regex.Match(content, matchedPattern);
                                if (match.Groups.Count > 1)
                                {
                                    for (int i = 1; i < match.Groups.Count; i++)
                                        args.AddRange(match.Groups[i].Value.Split(' '));
                                }
                            }
                            else
                            {
                                var split = content.Split(' ');
                                for (int i = 1; i < split.Length; i++)
                                    args.Add(split[i]);
                            }
                            args.RemoveAll(x => x.Trim() == "");

                            object? returnValue = null;

                            if (method.GetParameters().First().ParameterType == typeof(RinEvent))
                            {
                                if (method.GetParameters().Length == 1)
                                    returnValue = method.Invoke(module.ModuleClass, new object[] { rinEvent });
                                else
                                    returnValue = method.Invoke(module.ModuleClass, new object[] { rinEvent, args });
                            }
                            else
                            {
                                if (method.GetParameters().First().ParameterType == rinEvent.OriginalEvent.GetType())
                                {
                                    returnValue = method.Invoke(module.ModuleClass, new object[] { rinEvent.OriginalEvent, rinEvent.OriginalSender });
                                }
                            }


                            if (method.GetParameters().FirstOrDefault() != null
                                && method.GetParameters().FirstOrDefault().ParameterType == typeof(RinEvent)
                                && returnValue != null)
                            {
                                invokeRecord.IsInvoked = true;
                                var messageChain = new RinMessageChain();

                                switch (attr.ReplyType)
                                {
                                    case ReplyType.Send:
                                        break;

                                    case ReplyType.Reply:
                                        messageChain.Add(ReplyChain.Create(rinEvent.OriginalEvent));
                                        break;

                                    case ReplyType.At:
                                        messageChain.Add(Message.Model.AtChain.Create(rinEvent.SenderId));
                                        break;
                                }

                                if (returnValue is string)
                                {
                                    if ((returnValue as string) == "") return;
                                    messageChain.Add(TextChain.Create((string)returnValue));
                                    rinEvent.Reply(messageChain);
                                    return;
                                }

                                if (returnValue is RinMessageChain)
                                {
                                    messageChain = (RinMessageChain)returnValue;
                                    switch (attr.ReplyType)
                                    {
                                        case ReplyType.Send:
                                            break;

                                        case ReplyType.Reply:
                                            messageChain.Add(ReplyChain.Create(rinEvent.OriginalEvent));
                                            break;

                                        case ReplyType.At:
                                            messageChain.Add(Message.Model.AtChain.Create(rinEvent.SenderId));
                                            break;
                                    }
                                    rinEvent.Reply(messageChain);
                                    return;
                                }

                                if (returnValue is Konata.Core.Message.MessageChain)
                                {
                                    if (rinEvent.EventSourceType != EventSourceType.QQ)
                                        return;
                                    rinEvent.KonataMessageReply((Konata.Core.Message.MessageChain)returnValue);
                                    return;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error($"{e.Message}\n{e.StackTrace}\n{e.InnerException?.Message}\n{e.InnerException?.StackTrace}");
                            var messageChain = new RinMessageChain();
                            messageChain.Add(TextChain.Create($"[CMD]\n执行 {module.ModuleAttribute.ModuleName}({module.ModuleAttribute.ModuleID}:{command.Attribute.Name}({command.Method.Name})时发生错误\n请检查日志文件\n{e.InnerException?.GetType().ToString() ?? e.GetType().ToString()}: {e.InnerException?.Message ?? e.Message}"));
                            rinEvent.Reply(messageChain);
                        }
                        finally
                        {
                            RinDatabase.Instance.dbConnection.Insert(invokeRecord);
                            Logger.Info($"Command {attr.Name} completed");
                        }
                    }
                }
            }
        }
    }
}
