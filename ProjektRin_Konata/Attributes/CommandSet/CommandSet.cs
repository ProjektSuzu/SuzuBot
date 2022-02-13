namespace ProjektRin.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CommandSet : Attribute
    {
        public string Name;

        public CommandSet(string name)
        {
            this.Name = name;
        }
    }
}
