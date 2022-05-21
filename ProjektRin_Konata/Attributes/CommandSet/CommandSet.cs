namespace ProjektRin.Attributes.CommandSet
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CommandSet : Attribute
    {
        public string Name;
        public string PackageName;
        public CommandSet(string name, string packageName)
        {
            Name = name;
            PackageName = packageName;
        }
    }
}
