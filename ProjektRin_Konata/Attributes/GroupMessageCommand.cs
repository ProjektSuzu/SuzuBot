using System.Text.RegularExpressions;

namespace ProjektRin.Attributes
{
    public class GroupMessageCommand : MessageEventHandler
    {
        public GroupMessageCommand(string name, string description, string pattern) : base(name, description, pattern)
        {
        }
    }
}
