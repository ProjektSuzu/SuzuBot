using NLog;
using RinBot.Core.Components.Attributes;
using RinBot.Core.Components.Commands;
using RinBot.Core.Components.Databases.Tables;
using RinBot.Core.KonataCore.Contacts.Models;
using RinBot.Core.KonataCore.Events;
using System.Reflection;

namespace RinBot.Core.Components.Managers
{
    internal class CommandManager
    {
        #region Singleton
        public static CommandManager Instance = new Lazy<CommandManager>(() => new CommandManager()).Value;
        private CommandManager()
        {

        }
        #endregion

        public List<string> CommandPrefixs = new()
        {
            "/",
            "铃酱",
        };

        public Dictionary<string, BotModule> ModuleTable = new();
        public int ModuleCount => ModuleTable.Count;

        private Dictionary<string, BotCommand> commandTable = new();
        public int CommandCount => commandTable.Count;
        private static Logger Logger = LogManager.GetLogger("CMD");

        public void OnInit()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsClass)
                {
                    var attribute = type.GetCustomAttribute<ModuleAttribute>();
                    if (attribute != null)
                    {
                        var module = LoadModule(type, attribute);
                        var info = GlobalScope.DatabaseManager.DBConnection
                            .Table<ModuleInfo>()
                            .Where(x => x.ModuleId == module.ModuleId)
                            .FirstOrDefaultAsync().Result;
                        if (info == null)
                        {
                            info = new ModuleInfo()
                            {
                                Name = module.Name,
                                ModuleId = module.ModuleId,
                                IsEnabled = true,
                            };
                            _ = GlobalScope.RinDBAsyncConnection.InsertAsync(info);
                        }
                        module.IsEnabled = info.IsEnabled;
                        RegsiterModule(module);
                    }
                }
            }
            Logger.Info($"{ModuleTable.Count} Module(s), {commandTable.Count} Command(s) Loaded.");
        }

        public void OnBotCommand(MessageEventArgs messageEvent)
        {
            var rawContent = messageEvent.Message.Chain.ToString();
            var commandStruct = Interprete(rawContent);
            if (commandStruct == null) return;

            if (commandTable.TryGetValue(commandStruct.FuncToken, out var command))
            {
                var module = ModuleTable[command.ParentId];
                if (!module.IsEnabled) return;

                if (messageEvent.Subject is Group group)
                {
                    var groupInfo = GlobalScope.PermissionManager.GetGroupInfo(group.Uin);
                    switch (module.EnableType)
                    {
                        case ModuleEnableType.NormallyEnabled:
                            {
                                if (groupInfo.ModuleIds.Contains(module.ModuleId))
                                    return;
                                break;
                            }
                        case ModuleEnableType.NormallyDisabled:
                            {
                                if (!groupInfo.ModuleIds.Contains(module.ModuleId))
                                    return;
                                break;
                            }
                        case ModuleEnableType.WhiteListOnly:
                            {
                                if (!GlobalScope.PermissionManager.IsGroupInWhiteList(group.Uin).Result)
                                    return;
                                break;
                            }
                    }
                }

                var senderPermission = GlobalScope.PermissionManager.GetUserLevel(messageEvent.Sender);
                if (senderPermission < command.Permission)
                {
                    Logger.Warn($"{module.Name}|{command.Name} Permission Denied " +
                    $"{messageEvent.Subject.Name}({messageEvent.Subject.Uin})|{messageEvent.Sender.Name}({messageEvent.Sender.Uin}).");
                    messageEvent.Reply("你没有执行该命令的权限\n" +
                        $"{module.Name}|{command.Name} 要求 {command.Permission}\n" +
                        $"你的权限级别为 {senderPermission}");
                    return;
                }

                try
                {
                    switch (command.Method.GetParameters().Length)
                    {
                        case 2:
                            command.Method.Invoke(module.Instance, new object[] { messageEvent, commandStruct });
                            break;
                        case 1:
                            command.Method.Invoke(module.Instance, new object[] { messageEvent });
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                    Logger.Info($"{module.Name}|{command.Name} Invoked " +
                        $"{messageEvent.Subject.Name}({messageEvent.Subject.Uin})|{messageEvent.Sender.Name}({messageEvent.Sender.Uin}).");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error Occured When Execute {module.Name}|{command.Name} "
                                 + $"By {messageEvent.Subject.Name}({messageEvent.Subject.Uin})|{messageEvent.Sender.Name}({messageEvent.Sender.Uin})\n"
                                 + $"{ex.InnerException?.ToString()} {ex.InnerException?.Message}\n{ex.InnerException?.StackTrace}\n"
                                 + $"{ex} {ex.Message}\n{ex.StackTrace}");
                }
            }
            else
            {
                return;
            }
        }

        public bool HasPrefix(string command)
        {
            foreach (string prefix in CommandPrefixs)
            {
                if (command.TrimStart().StartsWith(prefix)) return true;
            }
            return false;
        }

        public string DropPrefix(string command)
        {
            command = command.Trim();
            foreach (var prefix in CommandPrefixs)
            {
                if (command.StartsWith(prefix))
                {
                    return command[prefix.Length..];
                }
            }
            return command;
        }

        public CommandStruct? Interprete(string command)
        {
            if (!HasPrefix(command)) return null;

            var array = DropPrefix(command).Split(' ', StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries).ToArray();
            if (array.Length <= 0) return null;

            var token = array[0];
            var args = array.Length > 1 ? array[1..] : Array.Empty<string>();
            return new CommandStruct()
            {
                FuncToken = token,
                FuncArgs = args,
            };
        }

        public ModuleInfo? GetModuleInfo(string moduleId)
        {
            if (ModuleTable.TryGetValue(moduleId, out var module))
            {
                return new ModuleInfo()
                {
                    Name = module.Name,
                    ModuleId = module.ModuleId,
                    IsEnabled = module.IsEnabled,
                    IsCritical = module.IsCritical,
                };
            }
            else
                return null;

        }

        public ModuleInfo? GetModuleInfoByName(string moduleName)
        {
            foreach (var module in ModuleTable)
            {
                if (module.Value.Name == moduleName)
                    return GetModuleInfo(module.Value.ModuleId);
            }
            return null;
        }

        public BotCommand? GetCommand(string command)
        {
            if (commandTable.TryGetValue(command, out var module)) return module;
            else return null;
        }

        public void ReloadModules()
        {
            commandTable.Clear();
            OnInit();
        }

        public void RegisterCommand(BotCommand command)
        {
            foreach (var func in command.Tokens)
            {
                commandTable.TryAdd(func, command);
            }
        }

        public void RegsiterModule(BotModule module)
        {
            foreach (var cmd in module.Commands)
            {
                RegisterCommand(cmd);
            }
            ModuleTable.TryAdd(module.ModuleId, module);
        }

        public BotModule LoadModule(Type type, ModuleAttribute attribute)
        {
            var methods = type.GetMethods();
            var commands = new List<BotCommand>();
            Logger.Info($"Loading Module: {attribute.Name}({attribute.ModuleId})");
            foreach (var method in methods)
            {
                CommandAttribute? cmdAttribute = method.GetCustomAttribute<CommandAttribute>();
                if (cmdAttribute == null) continue;
                commands.Add(LoadCommand(method, cmdAttribute, attribute.ModuleId));
            }
            var instance = Activator.CreateInstance(type);
            return new BotModule
                (
                    attribute.Name,
                    attribute.ModuleId,
                    attribute.IsCritical,
                    attribute.EnableType,
                    true,
                    instance,
                    commands
                );
        }

        public BotCommand LoadCommand(MethodInfo method, CommandAttribute attribute, string parentId)
        {
            Logger.Info($"\tLoading Command: {parentId}|{attribute.Name}");
            return new BotCommand
                (
                    attribute.Name,
                    attribute.FuncTokens,
                    attribute.Permission,
                    method,
                    parentId
                );
        }
    }
}
