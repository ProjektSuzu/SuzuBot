namespace RinBot.Core.Attributes.CommandSet
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class CommandSet : Attribute
    {
        public readonly string Name;
        public readonly string PackageName;
        public readonly bool DefaultEnable;

        public CommandSet(string name, string packageName, bool defaultEnable = true)
        {
            Name = name;
            PackageName = packageName;
            DefaultEnable = defaultEnable;
        }
    }
}
