namespace SuzuBot.Core.EventArgs.Bot;

public enum PokeType
{
    Friend,
    Group
}

public class PokeEventArgs : SuzuEventArgs
{
    public PokeType PokeType { get; set; }
    public uint SenderId { get; set; }
    public uint SubjectId { get; set; }
    public uint ReceiverId { get; set; }
}
