using System.Text.RegularExpressions;

namespace ProjektRin.Attributes.Command
{
    public class GroupMessageCommand : Command
    {
        public GroupMessageCommand(string name) : base(name)
        {
        }

        public GroupMessageCommand(string name, string pattern) : base(name, pattern)
        {
        }

        public GroupMessageCommand(string name, string[] pattern) : base(name, pattern)
        {
        }
    }
}
