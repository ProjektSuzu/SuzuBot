using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProjektRin.Attributes.Command
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]

    public class Command : Attribute
    {
        public string Name { get; }
        public List<Regex>? Patterns;

        public Command(string name, string pattern)
        {
            Name = name;
            Patterns = new List<Regex>();
            Patterns.Add(new Regex(pattern));
        }

        public Command(string name, string[] patterns)
        {
            Name = name;
            Patterns = patterns.Select(x => new Regex(x)).ToList();

        }

        public Command(string name)
        {
            Name = name;
            Patterns = null;
        }
    }
}
