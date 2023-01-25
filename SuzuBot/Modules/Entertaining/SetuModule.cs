using System.Linq;
using System.Net.Http.Json;
using Konata.Core.Message;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;
using SuzuBot.Utils;

namespace SuzuBot.Modules.Entertaining;
public class SetuModule : BaseModule
{
    private List<(uint Id, DateTime Time)> _cooldownList = new();
    HttpClient _httpClient;
    private const string acgUrl = @"https://www.loliapi.com/acg/pc/";
    private const string setuUrl = @"https://api.lolicon.app/setu/v2?r18=2&excludeAI=true&proxy=i.pixiv.cat&size=regular";
    private const int acgCost = 20;
    private const int acgCoolDownSeconds = 60;
    private const int setuCost = 20;
    private const int setuCoolDownSeconds = 60;

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

    [Command("色图", "^setu\\s*(.*)?")]
    public async Task Setu(MessageEventArgs eventArgs, string[] args)
    {
        int num = 1;
        string tag = string.Empty;
        if (args.Length > 0)
            tag = args[0];

        if (num <= 0 || num > 20)
        {
            await eventArgs.Reply(new MessageBuilder("[Setu]\n请求的数量过多"));
            return;
        }

        if (_cooldownList.Any(x => x.Id == eventArgs.Sender.Id))
        {
            var (_, Time) = _cooldownList.First(x => x.Id == eventArgs.Sender.Id);
            var seconds = (Time - DateTime.Now).TotalSeconds;
            await eventArgs.Reply($"∑(O_O；) 功能冷却中\n" +
                $"还需等待 {seconds:0.000} s");
            return;
        }

        var info = Context.DatabaseManager.GetUserInfo(eventArgs.Sender.Id);
        if (info.Coin <= setuCost * num)
        {
            await eventArgs.Reply($"∑(O_O；) SuzuCoin 不足\n" +
                $"该命令需要 {setuCost * num} SC\n" +
                $"你只有 {info.Coin} SC");
            return;
        }

        _cooldownList.Add((eventArgs.Sender.Id, DateTime.Now.AddSeconds(setuCoolDownSeconds * num)));
        info.Coin -= setuCost * num;
        _ = Context.DatabaseManager.UpdateUserInfo(info);

        var url = setuUrl;

        if (tag != string.Empty)
        {
            foreach (var t in tag.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                url += $"&tag={t}";
            }
        }

        if (num > 1)
        {
            url += $"&num={num}";
        }

        try
        {

            var json = await _httpClient.GetFromJsonAsync<SetuResult>(url);

            if (json is null)
            {
                await eventArgs.Reply(new MessageBuilder("[Setu]\n获取失败"));
                return;
            }

            if (json.error != string.Empty)
            {
                await eventArgs.Reply(new MessageBuilder("[Setu]\n获取失败\n" +
                    json.error));
                return;
            }

            if (json.data.Length <= 0)
            {
                await eventArgs.Reply(new MessageBuilder("[Setu]\n找不到包含指定标签的色图\n" +
                    tag));
                return;
            }

            List<TempMsgContent> tempMsgs = new();

            foreach (var setu in json.data)
            {
                byte[] bytes;
                try
                {
                    bytes = await _httpClient.GetByteArrayAsync(setu.urls.regular);
                }
                catch
                {
                    continue;
                }

                var b64Str = Convert.ToBase64String(bytes);
                tempMsgs.Add(new() { Type = MessageType.Text, Content = $"标题:\t{setu.title}" });
                tempMsgs.Add(new() { Type = MessageType.Text, Content = $"作者:\t{setu.author}" });
                tempMsgs.Add(new() { Type = MessageType.Text, Content = $"pid:\t{setu.pid}" });
                tempMsgs.Add(new() { Type = MessageType.Text, Content = $"标签:\t{string.Join(" #", setu.tags)}" });
                tempMsgs.Add(new() { Type = MessageType.Image, Content = b64Str });
                tempMsgs.Add(new() { Type = MessageType.Text, Content = "\n" });
            }

            var result = await TempMsgUtils.PostTempMsg(tempMsgs);
            if (result is not null)
            {
                await eventArgs.Reply(new MessageBuilder("[Setu]\n" +
                    result.Url));
            }
            else
            {
                await eventArgs.Reply(new MessageBuilder("[Setu]\n上传失败"));
            }
        }
        catch
        {
            info.Coin += setuCost * num;
            _ = Context.DatabaseManager.UpdateUserInfo(info);
            _cooldownList.RemoveAll(x => x.Id == eventArgs.Sender.Id);
            throw;
        }
    }
}


public class SetuResult
{
    public string error { get; set; }
    public SetuData[] data { get; set; }
}

public class SetuData
{
    public int pid { get; set; }
    public int p { get; set; }
    public int uid { get; set; }
    public string title { get; set; }
    public string author { get; set; }
    public bool r18 { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public string[] tags { get; set; }
    public string ext { get; set; }
    public int aiType { get; set; }
    public long uploadDate { get; set; }
    public Urls urls { get; set; }
}

public class Urls
{
    public string regular { get; set; }
}
