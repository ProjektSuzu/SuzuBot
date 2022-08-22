using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Core.KonataCore.Contacts
{
    internal abstract class BotContact
    {
        public string Name { get; internal set; }
        public uint Uin { get; internal set; }

        protected BotContact(string name, uint uin)
        {
            Name = name;
            Uin = uin;
        }
    }
}
