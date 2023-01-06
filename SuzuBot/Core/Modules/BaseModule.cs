using SuzuBot.Core.Contacts;

namespace SuzuBot.Core.Modules;
public abstract class BaseModule
{
    public Context Context { get; set; }
    public string Name { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsCritical { get; set; }

    public virtual bool Init() => true;
    public virtual bool Enable()
    {
        IsEnabled = true;
        return IsEnabled;
    }
    public virtual bool Disable()
    {
        IsEnabled = false;
        return IsEnabled;
    }
    public virtual bool Destory() => true;

}
