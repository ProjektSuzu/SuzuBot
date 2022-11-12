
using System.Text;
using Konata.Core.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SuzuBot.Common;
using SuzuBot.Common.Attributes;
using SuzuBot.Common.EventArgs.Messages;
using MatchType = SuzuBot.Common.Attributes.MatchType;

namespace SuzuBot.Modules;

[Module("疫情")]
internal class PandemicModule : BaseModule
{
    private const string _url = "http://111.231.75.86:8000/api/cities/CHN/?cityNames=";
    private HttpClient _httpClient;

    public override void Init()
    {
        base.Init();
        _httpClient = new HttpClient()
        {
            Timeout = new TimeSpan(0, 1, 0)
        };
    }

    [Command("疫情查询", "pandemic", "疫情", MatchType = MatchType.StartsWith)]
    public async Task Pandemic(MessageEventArgs eventArgs, string[] args)
    {
        var city = args[0];
        var result = await GetPandemicInfo(city);
        if (result is null)
        {
            await eventArgs.Reply(new MessageBuilder("[Pandemic]\n无法识别的城市名"));
            return;
        }

        StringBuilder stringBuilder = new StringBuilder("[Pandemic]\n");
        stringBuilder.AppendLine($"地区：{result.ProvinceName} {result.CityName}");
        stringBuilder.AppendLine($"今日确诊病例：{result.CurrentConfirmedCount}");
        stringBuilder.AppendLine($"确诊病例：{result.ConfirmedCount}");
        stringBuilder.AppendLine($"疑似病例：{result.SuspectedCount}");
        stringBuilder.AppendLine($"治愈病例：{result.CuredCount}");
        stringBuilder.AppendLine($"死亡病例：{result.DeadCount}");
        await eventArgs.Reply(new MessageBuilder(stringBuilder.ToString()));
    }

    public async Task<PandemicInfo?> GetPandemicInfo(string cityName)
    {
        var url = _url + cityName;
        var result = await _httpClient.GetAsync(url);
        if (result is null || !result.IsSuccessStatusCode)
            return null;

        var bytes = await result.Content.ReadAsByteArrayAsync();
        var content = Encoding.UTF8.GetString(bytes);
        var array = JsonConvert.DeserializeObject<PandemicInfo[]>(content);
        if (array is null || !array.Any())
            return null;

        return array.FirstOrDefault();
    }
}

[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class PandemicInfo
{
    public string ProvinceCode { get; set; }
    public string ProvinceName { get; set; }
    public string CityName { get; set; }
    public int CurrentConfirmedCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int SuspectedCount { get; set; }
    public int CuredCount { get; set; }
    public int DeadCount { get; set; }
}
