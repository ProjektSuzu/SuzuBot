namespace RinBot.Core.Components.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class PokeCommandAttribute : CommandHandlerAttribute
    {
        public PokeReceiveTarget ReceiveTarget { get; protected set; }

        public PokeCommandAttribute(string name, PokeReceiveTarget receiveTarget = PokeReceiveTarget.Any)
        {
            Name = name;
            ReceiveTarget = receiveTarget;
        }
    }
}
