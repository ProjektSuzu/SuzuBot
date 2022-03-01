using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektRin.System
{
    internal class GroupPreference
    {
        public uint GroupUin;
        public bool PassiveMode = false;

        public List<string> DisabledCommandSets = new();

        public GroupPreference(uint groupUin) => GroupUin = groupUin;
    }
}
