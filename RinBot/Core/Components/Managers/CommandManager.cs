using RinBot.Core.Components.Attributes;
using RinBot.Core.Components.Commands;
using RinBot.Core.Components.Databases.Tables;
using System.Reflection;
using Module = RinBot.Core.Components.Commands.Module;

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

        private Dictionary<string, Module> moduleTable = new();
        private Dictionary<string, Command> commandTable = new();

        public async void OnInit()
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
                        var info = await GlobalScope.DatabaseManager.DBConnection
                            .Table<ModuleInfo>()
                            .Where(x => x.ModuleId == module.ModuleId)
                            .FirstAsync();
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
        }

        public ModuleInfo? GetModuleInfo(string moduleId)
        {
            if (!moduleTable.TryGetValue(moduleId, out var module))
            {
                return new ModuleInfo()
                {
                    Name = module.Name,
                    ModuleId = module.ModuleId,
                    IsEnabled = module.IsEnabled,
                };
            }
            else
                return null;

        }

        public Command? GetCommand(string command)
        {
            if (commandTable.TryGetValue(command, out var module)) return module;
            else return null;
        }

        public void ReloadModules()
        {
            commandTable.Clear();
        }

        public void RegisterCommand(Command command)
        {
            foreach (var func in command.FuncNames)
            {
                commandTable.TryAdd(func, command);
            }
        }

        public void RegsiterModule(Module module)
        {
            foreach (var cmd in module.Commands)
            {
                RegisterCommand(cmd);
            }
            moduleTable.TryAdd(module.ModuleId, module);
        }

        public Module LoadModule(Type type, ModuleAttribute attribute)
        {
            var methods = type.GetMethods();
            var commands = new List<Command>();
            foreach (var method in methods)
            {
                CommandAttribute? cmdAttribute = method.GetCustomAttribute<CommandAttribute>();
                if (cmdAttribute != null) continue;
                commands.Add(LoadCommand(method, cmdAttribute, attribute.ModuleId));
            }
            var instance = Activator.CreateInstance(type);
            return new Module
                (
                    attribute.Name,
                    attribute.ModuleId,
                    attribute.IsCritical,
                    true,
                    instance,
                    commands
                );
        }

        public Command LoadCommand(MethodInfo method, CommandAttribute attribute, string parentId)
        {
            return new Command
                (
                    attribute.Name,
                    attribute.FuncNames,
                    attribute.Permission,
                    method,
                    parentId
                );
        }
    }
}
