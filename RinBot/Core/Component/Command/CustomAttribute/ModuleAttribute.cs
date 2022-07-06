namespace RinBot.Core.Component.Command.CustomAttribute
{
    public enum ModuleEnableConfig
    {
        NormallyEnable,
        WhiteListOnly,
        NormallyDisable,
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class ModuleAttribute : Attribute
    {
        public string ModuleName { get; private set; }
        public string ModuleID { get; private set; }
        public ModuleEnableConfig ModuleEnableConfig { get; private set; }
        public bool CriticalModule { get; private set; }

        public ModuleAttribute(string moduleName, string moduleID, ModuleEnableConfig moduleEnableConfig = ModuleEnableConfig.NormallyEnable, bool critical = false)
        {
            ModuleName = moduleName;
            ModuleID = moduleID;
            ModuleEnableConfig = moduleEnableConfig;
            CriticalModule = critical;
        }
    }
}
