namespace RinBot.Core.Components.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class ModuleAttribute : Attribute
    {
        public string Name { get; protected set; }
        public string ModuleId { get; protected set; }
        public bool IsCritical { get; protected set; }
        public ModuleEnableType EnableType { get; protected set; }

        public ModuleAttribute(string name, string moduleId, bool isCritical = false, ModuleEnableType enableType = ModuleEnableType.NormallyEnabled)
        {
            Name = name;
            ModuleId = moduleId;
            IsCritical = isCritical;
            EnableType = enableType;
        }
    }
}
