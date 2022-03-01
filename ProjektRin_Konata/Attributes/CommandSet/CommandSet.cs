namespace ProjektRin.Attributes.CommandSet
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CommandSet : Attribute
    {
        public string Name;

        public CommandSet(string name)
        {
            Name = name;
        }
    }
}
