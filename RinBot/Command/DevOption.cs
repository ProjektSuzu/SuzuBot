using RinBot.BuildStamp;
using RinBot.Core.Component.Command.CustomAttribute;
using RinBot.Core.Component.Event;

namespace RinBot.Command
{
    [Module("开发者选项", "org.akulak.devOption")]
    internal class DevOption
    {
        [Command("环境变量", "", MatchingType.StartsWith, ReplyType.Reply)]
        public string OnHelp(RinEvent e)
        {
            return $"[RinBot] {RinBotBuildStamp.Version}\n请访问 https://docs-rinbot.akulak.icu 来获取帮助信息";
        }
    }
}
