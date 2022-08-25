namespace RinBot.Core.Components.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class CommandHandlerAttribute : Attribute
    {
        public string Name { get; protected set; }
    }
}
