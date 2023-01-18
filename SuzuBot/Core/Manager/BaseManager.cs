using Microsoft.Extensions.Logging;
using SuzuBot.Utils;

namespace SuzuBot.Core.Manager;
public abstract class BaseManager
{
    protected Context Context;
    protected ILogger Logger;

    public BaseManager(Context context)
    {
        Context = context;
        Logger = LogUtils.CreateLogger(this.ToString());
    }
}
