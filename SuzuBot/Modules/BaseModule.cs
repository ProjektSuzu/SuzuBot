namespace SuzuBot.Extensions;
internal class BaseModule
{
    protected readonly IServiceProvider _serviceProvider;

    public BaseModule(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
}
