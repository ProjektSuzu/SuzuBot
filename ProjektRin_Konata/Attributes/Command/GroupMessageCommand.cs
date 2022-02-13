using System.Text.RegularExpressions;

namespace ProjektRin.Attributes
{
    public class GroupMessageCommand : Command
    {
        public GroupMessageCommand(string name, string description, string usage) : base(name, description, usage)
        {
        }

        public GroupMessageCommand(string name, string description, string usage, string pattern) : base(name, description, usage, pattern)
        {
        }

        public GroupMessageCommand(string name, string description, string usage, string[] pattern) : base(name, description, usage, pattern)
        {
        }
    }
}
