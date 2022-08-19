using RinBot.Core.Components.Databases.Tables;

namespace RinBot.Core.Components.Commands
{
    internal class Module
    {
        public string Name { get; protected set; }
        public string ModuleId { get; protected set; }
        public bool IsCritical { get; protected set; }

        private bool isEnabled;
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                isEnabled = value;
                var info = GlobalScope.RinDBAsyncConnection
                    .Table<ModuleInfo>()
                    .Where(x => x.ModuleId == ModuleId)
                    .FirstAsync()
                    .Result;
                info.IsEnabled = value;
                GlobalScope.RinDBAsyncConnection
                    .UpdateAsync(info);
            }
        }
        public object Instance { get; protected set; }
        public List<Command> Commands { get; protected set; }

        public Module(string name, string moduleId, bool isCritical, bool isEnabled, object instance, List<Command> commands)
        {
            Name = name;
            ModuleId = moduleId;
            IsCritical = isCritical;
            this.isEnabled = isEnabled;
            Instance = instance;
            Commands = commands;
        }
    }
}
