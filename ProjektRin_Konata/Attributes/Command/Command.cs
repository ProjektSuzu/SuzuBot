using System.Text.RegularExpressions;
using static ProjektRin.Components.PermissionManager;

namespace ProjektRin.Attributes.Command
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]

    public class Command : Attribute
    {
        public string Name { get; }
        public List<Regex>? Patterns;
        public Permission Permission;
        public bool IsRaw;

        public Command(string name, string pattern, Permission permission = Permission.User, bool isRaw = false)
        {
            Name = name;
            Patterns = new List<Regex>
            {
                new Regex(pattern)
            };
            Permission = permission;
            IsRaw = isRaw;

        }

        public Command(string name, string[] patterns, Permission permission = Permission.User, bool isRaw = false)
        {
            Name = name;
            Patterns = patterns.Select(x => new Regex(x)).ToList();
            Permission = permission;
            IsRaw = isRaw;
        }

        public Command(string name, Permission permission = Permission.User, bool isRaw = false)
        {
            Name = name;
            Patterns = null;
            Permission = permission;
            IsRaw = isRaw;
        }
    }
}
