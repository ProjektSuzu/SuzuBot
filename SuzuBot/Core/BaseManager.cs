namespace SuzuBot.Core;
internal abstract class BaseManager
{
    protected Context Context { get; set; }

    protected BaseManager(Context context)
    {
        Context = context;
    }
}
