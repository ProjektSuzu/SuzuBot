using ProjektRin.Core.Components;
using System.Text.RegularExpressions;

namespace ProjektRin.Core.Attributes.Command.Modules
{
    internal class MessageCommand : Command
    {
        public readonly List<Regex> Regexs;
        public readonly bool IsRaw;

        protected MessageCommand(string name, Permission permission, bool isRaw, string[] regexs) : base(name, permission)
        {
            Regexs = new List<Regex>();
            foreach (var regex in regexs)
            {
                Regexs.Add(new Regex(regex));
            }
            IsRaw = isRaw;
        }

        protected MessageCommand(string name, Permission permission, string[] regexs) : base(name, permission)
        {
            Regexs = new List<Regex>();
            foreach (var regex in regexs)
            {
                Regexs.Add(new Regex(regex));
            }
            IsRaw = false;
        }
        protected MessageCommand(string name, string[] regexs) : base(name, Permission.User)
        {
            Regexs = new List<Regex>();
            foreach (var regex in regexs)
            {
                Regexs.Add(new Regex(regex));
            }
            IsRaw = false;
        }
    }
}
