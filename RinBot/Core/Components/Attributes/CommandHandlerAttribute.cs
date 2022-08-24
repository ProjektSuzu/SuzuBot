using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Core.Components.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class CommandHandlerAttribute : Attribute
    {
        public string Name { get; protected set; }
    }
}
