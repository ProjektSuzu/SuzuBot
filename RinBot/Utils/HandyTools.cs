using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektRin.Utils
{
    internal static class HandyTools
    {
        public static void Reply(this GroupMessageEvent groupMessageEvent, Bot bot, MessageBuilder builder)
        {
            bot.SendGroupMessage(groupMessageEvent.GroupUin, builder);
        }
    }
}
