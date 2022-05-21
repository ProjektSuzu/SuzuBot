using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;

namespace RinBot.Utils
{
    internal static class HandyTools
    {
        public static void Reply(this GroupMessageEvent groupMessageEvent, Bot bot, MessageBuilder builder)
        {
            bot.SendGroupMessage(groupMessageEvent.GroupUin, builder);
        }
    }
}
