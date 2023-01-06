namespace SuzuBot.Core.Contacts;
public abstract class Contact
{
    public required uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}
