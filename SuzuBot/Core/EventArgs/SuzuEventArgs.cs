namespace SuzuBot.Core.EventArgs;

public abstract class SuzuEventArgs : System.EventArgs
{
    public Konata.Core.Bot Bot { get; init; }
    public string EventId { get; init; }
    public DateTime EventTime { get; init; }

    public SuzuEventArgs()
    {
        EventId = Guid.NewGuid().ToString();
        EventTime = DateTime.Now;
    }
}
