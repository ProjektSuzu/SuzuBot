using RinBot.Core.Component.Permission;

namespace RinBot.Core.Component.Command.CustomAttribute
{
    public enum MatchingType
    {
        Exact = 1,
        StartsWith = 2,
        Always = 4,
        Contains = 8,
        Regex = 16,
        NoLeadChar = 32,
    }

    public enum ReplyType
    {
        Send = 1,
        Reply = 2,
        At = 4,
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal class CommandAttribute : Attribute
    {
        public string Name { get; private set; }
        public string[] Command { get; private set; }
        public int MatchingMask { get; private set; }
        public int EventSourceMask { get; private set; }
        public ReplyType ReplyType { get; private set; }
        public UserRole Role { get; private set; }
        public bool WhiteListOnly { get; private set; }

        public CommandAttribute(string name, string command, MatchingType matchingType, ReplyType replyType, UserRole userRole = UserRole.User, bool whiteListOnly = false, int eventSourceMask = 0b11)
        {
            Name = name;
            Command = new[] { command };
            MatchingMask = (int)matchingType;
            ReplyType = replyType;
            Role = userRole;
            WhiteListOnly = whiteListOnly;
            EventSourceMask = eventSourceMask;
        }

        public CommandAttribute(string name, string[] command, int matchingMask, ReplyType replyType, UserRole userRole = UserRole.User, bool whiteListOnly = false, int eventSourceMask = 0b11)
        {
            Name = name;
            Command = command;
            MatchingMask = matchingMask;
            ReplyType = replyType;
            Role = userRole;
            WhiteListOnly = whiteListOnly;
            EventSourceMask = eventSourceMask;
        }

        public CommandAttribute(string name, string command, int matchingMask, ReplyType replyType, UserRole userRole = UserRole.User, bool whiteListOnly = false, int eventSourceMask = 0b11)
        {
            Name = name;
            Command = new[] { command };
            MatchingMask = matchingMask;
            ReplyType = replyType;
            Role = userRole;
            WhiteListOnly = whiteListOnly;
            EventSourceMask = eventSourceMask;
        }
    }
}
