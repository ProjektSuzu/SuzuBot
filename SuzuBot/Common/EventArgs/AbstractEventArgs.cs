using Konata.Core;

#pragma warning disable CS8618

namespace SuzuBot.Common.EventArgs;
public abstract class AbstractEventArgs : System.EventArgs
{
    public DateTime EventTime { get; set; }
    public string EventMessage { get; set; }
    public Bot Bot { get; set; }
}
