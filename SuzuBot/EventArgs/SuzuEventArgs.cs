using Konata.Core;
using SuzuBot.Events;
using SuzuBot.Hosts;

namespace SuzuBot.EventArgs;
internal class SuzuEventArgs : System.EventArgs
{
    public EventBus EventBus { get; init; }
    public Bot Bot { get; init; }
}
