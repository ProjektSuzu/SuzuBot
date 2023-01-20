using Konata.Core.Message;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;

namespace SuzuBot.Modules.Entertaining;
public class OTTOModule : BaseModule
{
    private HttpClient _httpClient = new HttpClient()
    {
        BaseAddress = new Uri(@"https://www.aolianfeiallin.top/"),
        Timeout = TimeSpan.FromSeconds(60)
    };

    public OTTOModule()
    {
        Name = "电棍活字印刷";
    }

    [Command("说的道理", "^sddl$")]
    public async Task RollMemberAsWaifu(GroupMessageEventArgs eventArgs, string[] args)
    {
        var builder = new MessageBuilder();
        builder.Record(Path.Combine(ResourceDirPath, "sddl.mp3"));
        await eventArgs.SendMessage(builder);
    }

}
