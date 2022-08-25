using RinBot.Core.Components.Databases.Tables;

namespace RinBot.Core.Components.Commands
{
    internal class BotModule
    {
        public string Name { get; protected set; }
        public string ModuleId { get; protected set; }
        public bool IsCritical { get; protected set; }

        private bool isEnabled;
        public ModuleEnableType EnableType { get; protected set; }
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                isEnabled = value;
                var info = GlobalScope.RinDBAsyncConnection
                    .Table<ModuleInfo>()
                    .Where(x => x.ModuleId == ModuleId)
                    .FirstOrDefaultAsync()
                    .Result;
                if (info == null)
                {
                    info = new() { Name = this.Name, ModuleId = this.ModuleId, IsEnabled = value };
                    GlobalScope.RinDBAsyncConnection
                        .InsertAsync(info);
                }
                else
                {
                    info.IsEnabled = value;
                    GlobalScope.RinDBAsyncConnection
                        .UpdateAsync(info);
                }
            }
        }
        public object Instance { get; protected set; }
        public List<BotCommand> Commands { get; protected set; }

        public BotModule(string name, string moduleId, bool isCritical, ModuleEnableType enableType, bool isEnabled, object instance, List<BotCommand> commands)
        {
            Name = name;
            ModuleId = moduleId;
            IsCritical = isCritical;
            EnableType = enableType;
            this.isEnabled = isEnabled;
            Instance = instance;
            Commands = commands;
        }
    }
}
