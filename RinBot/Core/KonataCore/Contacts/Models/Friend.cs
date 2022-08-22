using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Core.KonataCore.Contacts.Models
{
    internal class Friend : BotContact
    {
        public string Remark { get; internal set; }
        internal Friend(string name, uint uin) : base(name, uin)
        {
           
        }
    }
}
