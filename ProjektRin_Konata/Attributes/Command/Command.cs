using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProjektRin.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]

    public class Command : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public string Usage { get; }
        public List<Regex>? Patterns;

        public Command(string name, string description, string usage, string pattern)
        {
            this.Name = name;
            this.Description = description;
            this.Usage = usage;
            this.Patterns = new List<Regex>();
            Patterns.Add(new Regex(pattern));
        }

        public Command(string name, string description, string usage, string[] patterns)
        {
            this.Name = name;
            this.Description = description;
            this.Usage = usage;
            this.Patterns = patterns.Select(x => new Regex(x)).ToList();

        }

        public Command(string name, string description, string usage)
        {
            Name = name;
            Description = description;
            this.Usage = usage;
            this.Patterns = null;
        }
    }
}
