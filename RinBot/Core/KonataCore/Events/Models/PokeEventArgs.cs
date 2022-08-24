
using RinBot.Core.Components;
using RinBot.Core.KonataCore.Contacts;
using RinBot.Core.KonataCore.Contacts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Core.KonataCore.Events
{
    internal class PokeEventArgs : RinEventArgs
    {
        public BotContact Subject { get; internal set; }
        public BotContact Sender { get; internal set; }
        // 这玩意是 null 说明接收人是 Bot
        public BotContact Receiver { get; internal set; }
        public SubjectType SubjectType
        {
            get
            {
                if (Subject is Group)
                {
                    return SubjectType.Group;
                }
                else
                {
                    return SubjectType.Friend;
                }
            }
        }
    }
}
