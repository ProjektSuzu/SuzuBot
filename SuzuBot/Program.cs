using Microsoft.Extensions.Hosting;
using SuzuBot.Extensions;
using SuzuBot.Hosting;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = new SuzuAppHostBuilder(args);
        var app = builder.Build();
        app.UseMetrics().UseCache().UsePrefixCheck().UseRoute().UseRuleCheck().UseInvoke().Run();
    }
}
