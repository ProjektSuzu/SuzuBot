using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProjektRin.Attributes
{
    public class MessageEventHandler : EventHandler
    {
        public string Name { get; }
        public string Description { get; }
        public Regex Pattern { get; }

        public MessageEventHandler(string name, string description, string pattern)
        {
            this.Name = name;
            this.Description = description;
            this.Pattern = new Regex(pattern);
        }
    }
}
