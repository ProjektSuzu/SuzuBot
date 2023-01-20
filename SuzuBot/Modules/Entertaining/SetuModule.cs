using Konata.Core.Message;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;

namespace SuzuBot.Modules.Entertaining;
public class SetuModule : BaseModule
{
    private List<(uint Id, DateTime Time)> _cooldownList = new();
    HttpClient _httpClient;
    private const string acgUrl = @"https://www.loliapi.com/acg/pc/";
    private const int acgCost = 20;
    private const int acgCoolDownSeconds = 60;

    private Timer _timer;

    public SetuModule()
    {
        Name = "色图";
        _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public override bool Enable()
    {
        _timer = new Timer(new TimerCallback((_) => Flush()));
        _timer.Change(new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 10));
        return base.Enable();
    }

    public override bool Disable()
    {
        _timer.Dispose();
        return base.Disable();
    }

    private void Flush()
    {
        _cooldownList = _cooldownList.Where(x => x.Time > DateTime.Now).ToList();
    }

    [Command("ACG图片", "^acg$")]
    public async Task ACGImage(MessageEventArgs eventArgs, string[] args)
    {
        if (_cooldownList.Any(x => x.Id == eventArgs.Sender.Id))
        {
            var (_, Time) = _cooldownList.First(x => x.Id == eventArgs.Sender.Id);
            var seconds = (Time - DateTime.Now).TotalSeconds;
            await eventArgs.Reply($"∑(O_O；) 功能冷却中\n" +
                $"还需等待 {seconds:0.000} s");
            return;
        }

        var info = Context.DatabaseManager.GetUserInfo(eventArgs.Sender.Id);
        if (info.Coin <= acgCost)
        {
            await eventArgs.Reply($"∑(O_O；) SuzuCoin 不足\n" +
                $"该命令需要 {acgCost} SC\n" +
                $"你只有 {info.Coin} SC");
            return;
        }

        _cooldownList.Add((eventArgs.Sender.Id, DateTime.Now.AddSeconds(acgCoolDownSeconds)));
        info.Coin -= acgCost;
        _ = Context.DatabaseManager.UpdateUserInfo(info);

        try
        {
            var bytes = await _httpClient.GetByteArrayAsync(acgUrl);
            await eventArgs.Reply(new MessageBuilder("[ACG]\n").Image(bytes));
            return;
        }
        catch (Exception ex)
        {
            info.Coin += acgCost;
            _ = Context.DatabaseManager.UpdateUserInfo(info);
            _cooldownList.RemoveAll(x => x.Id == eventArgs.Sender.Id);
            throw;
        }
    }
}
