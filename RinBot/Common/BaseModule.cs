using RinBot.Core;

namespace RinBot.Common;
internal abstract class BaseModule
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Context Context { get; set; }
    public string ResourceDirPath { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsCritical { get; internal set; }

    public virtual void Init()
    {
        if (!Directory.Exists(ResourceDirPath))
            Directory.CreateDirectory(ResourceDirPath);
    }
    public virtual bool Enable()
    {
        IsEnabled = true;
        return true;
    }
    public virtual void Disable()
    {
        if (!IsCritical)
            IsEnabled = false;
    }
}
