using Konata.Core;

namespace RinBot.Common.EventArgs;
public abstract class AbstractEventArgs : System.EventArgs
{
    public DateTime EventTime { get; set; }
    public string EventMessage { get; set; }
    public Bot Bot { get; set; }
}
