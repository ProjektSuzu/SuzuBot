using System.Text;
using System.Text.Json;
using Konata.Core.Message;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;
using SuzuBot.Utils;

namespace SuzuBot.Modules.Useful;
public class SatelliteModule : BaseModule
{
    private HttpClient _httpClient;
    private const int _issId = 25544;
    private const string _orbitDisplayUrl = @"https://www.heavens-above.com/orbitdisplay.aspx?icon=default&width=512&height=512&satid={satid}";
    private const string _orbitSatidUrl = @"https://celestrak.org/NORAD/elements/gp.php?CATNR={satid}&FORMAT=json";
    private const string _orbitNameUrl = @"https://celestrak.org/NORAD/elements/gp.php?NAME={name}&FORMAT=json";
    private const string _orbitIdUrl = @"https://celestrak.org/NORAD/elements/gp.php?INTDES={id}&FORMAT=json";

    public SatelliteModule()
    {
        Name = "卫星观测";
        _httpClient = new()
        {
            Timeout = new TimeSpan(0, 0, 5),
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/110.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
    }

    [Command("卫星轨道", "^orbit ([0-9]+)")]
    public async Task OrbitBySatId(MessageEventArgs eventArgs, string[] args)
    {
        int satid;
        var builder = new StringBuilder("[Satellite]Orbit\n");
        if (!int.TryParse(args[0], out satid))
        {
            builder.AppendLine($"空间飞行器目录编号格式错误: {args[0]}");
            await eventArgs.Reply(new MessageBuilder(builder.ToString()));
            return;
        }
        var sateInfo = GetSatelliteInfo(satid);
        if (sateInfo is null) 
        {
            builder.AppendLine($"找不到卫星或卫星不再活跃: {satid}");
            await eventArgs.Reply(new MessageBuilder(builder.ToString()));
            return;
        }
        var sateView = await GetSatelliteView(satid);

        builder.AppendLine($"空间飞行器目录编号\t{sateInfo.NORAD_CAT_ID}");
        builder.AppendLine($"空间飞行器目录名称\t{sateInfo.OBJECT_NAME}");
        builder.AppendLine($"国际卫星标识符\t{sateInfo.OBJECT_ID}");
        builder.AppendLine($"卫星类别\t{sateInfo.CLASSIFICATION_TYPE}");
        builder.AppendLine();
        builder.AppendLine($"UTC时间\t{sateInfo.EPOCH}");
        builder.AppendLine($"轨道偏心率\t{sateInfo.ECCENTRICITY}");
        builder.AppendLine($"轨道倾角\t{sateInfo.INCLINATION}");
        builder.AppendLine($"升交点赤经\t{sateInfo.RA_OF_ASC_NODE}");
        builder.AppendLine($"近地点幅角\t{sateInfo.ARG_OF_PERICENTER}");
        builder.AppendLine($"每日绕地圈数\t{sateInfo.MEAN_MOTION}");

        await eventArgs.Reply(new MessageBuilder(builder.ToString()).Image(sateView));
    }

    [Command("卫星轨道", "^orbit (.+)", Priority = 200)]
    public async Task OrbitBySatName(MessageEventArgs eventArgs, string[] args)
    {
        string name = args[0];
        var builder = new StringBuilder("[Satellite]Orbit\n");
        var sateInfo = GetSatelliteInfoByName(name);
        if (sateInfo is null)
        {
            builder.AppendLine($"找不到卫星或卫星不再活跃: {name}");
            await eventArgs.Reply(new MessageBuilder(builder.ToString()));
            return;
        }
        var sateView = await GetSatelliteView(sateInfo.NORAD_CAT_ID);

        builder.AppendLine($"空间飞行器目录编号\t{sateInfo.NORAD_CAT_ID}");
        builder.AppendLine($"空间飞行器目录名称\t{sateInfo.OBJECT_NAME}");
        builder.AppendLine($"国际卫星标识符\t{sateInfo.OBJECT_ID}");
        builder.AppendLine($"卫星类别\t{sateInfo.CLASSIFICATION_TYPE}");
        builder.AppendLine();
        builder.AppendLine($"UTC时间\t{sateInfo.EPOCH}");
        builder.AppendLine($"轨道偏心率\t{sateInfo.ECCENTRICITY}");
        builder.AppendLine($"轨道倾角\t{sateInfo.INCLINATION}");
        builder.AppendLine($"升交点赤经\t{sateInfo.RA_OF_ASC_NODE}");
        builder.AppendLine($"近地点幅角\t{sateInfo.ARG_OF_PERICENTER}");
        builder.AppendLine($"每日绕地圈数\t{sateInfo.MEAN_MOTION}");

        await eventArgs.Reply(new MessageBuilder(builder.ToString()).Image(sateView));
    }

    [Command("卫星轨道", "^orbit ([0-9]{4}-.+)", Priority = 200)]
    public async Task OrbitByIntdes(MessageEventArgs eventArgs, string[] args)
    {
        string intdes = args[0];
        var builder = new StringBuilder("[Satellite]Orbit\n");
        var sateInfo = GetSatelliteInfoByIntdes(intdes);
        if (sateInfo is null)
        {
            builder.AppendLine($"找不到卫星或卫星不再活跃: {intdes}");
            await eventArgs.Reply(new MessageBuilder(builder.ToString()));
            return;
        }
        var sateView = await GetSatelliteView(sateInfo.NORAD_CAT_ID);

        builder.AppendLine($"空间飞行器目录编号\t{sateInfo.NORAD_CAT_ID}");
        builder.AppendLine($"空间飞行器目录名称\t{sateInfo.OBJECT_NAME}");
        builder.AppendLine($"国际卫星标识符\t{sateInfo.OBJECT_ID}");
        builder.AppendLine($"卫星类别\t{sateInfo.CLASSIFICATION_TYPE}");
        builder.AppendLine();
        builder.AppendLine($"UTC时间\t{sateInfo.EPOCH}");
        builder.AppendLine($"轨道偏心率\t{sateInfo.ECCENTRICITY}");
        builder.AppendLine($"轨道倾角\t{sateInfo.INCLINATION}");
        builder.AppendLine($"升交点赤经\t{sateInfo.RA_OF_ASC_NODE}");
        builder.AppendLine($"近地点幅角\t{sateInfo.ARG_OF_PERICENTER}");
        builder.AppendLine($"每日绕地圈数\t{sateInfo.MEAN_MOTION}");

        await eventArgs.Reply(new MessageBuilder(builder.ToString()).Image(sateView));
    }

    [Command("ISS信息", "^iss$")]
    public Task ISS(MessageEventArgs eventArgs, string[] args)
    {
        return OrbitBySatId(eventArgs, new[] { "25544" });
    }

    private Task<byte[]> GetSatelliteView(int satid)
    {
        var url = _orbitDisplayUrl.Replace("{satid}", satid.ToString());
        if (satid == _issId)
            url = url.Replace("icon=default", "icon=iss");
        return _httpClient.GetByteArrayAsync(url);
    }

    private SatelliteOrbitData? GetSatelliteInfo(int satid)
    {
        var url = _orbitSatidUrl.Replace("{satid}", satid.ToString());
        var result = _httpClient.GetStringAsync(url).Result;
        try
        {
            var list = JsonSerializer.Deserialize<SatelliteOrbitData[]>(result);
            return list.FirstOrDefault();
        }
        catch (JsonException ex)
        {
            return null;
        }
        catch
        {
            throw;
        }
    }
    private SatelliteOrbitData? GetSatelliteInfoByName(string name)
    {
        name = name.ToUpper();
        var url = _orbitNameUrl.Replace("{name}", name.ToString());
        var result = _httpClient.GetStringAsync(url).Result;
        try
        {
            var list = JsonSerializer.Deserialize<SatelliteOrbitData[]>(result);
            return list.FirstOrDefault();
        }
        catch (JsonException ex)
        {
            return null;
        }
        catch
        {
            throw;
        }
    }
    private SatelliteOrbitData? GetSatelliteInfoByIntdes(string id)
    {
        id = id.ToUpper();
        var url = _orbitIdUrl.Replace("{id}", id.ToString());
        var result = _httpClient.GetStringAsync(url).Result;
        try
        {
            var list = JsonSerializer.Deserialize<SatelliteOrbitData[]>(result);
            return list.FirstOrDefault();
        }
        catch (JsonException ex)
        {
            return null;
        }
        catch
        {
            throw;
        }
    }
}

public record SatelliteOrbitData
{
    public string OBJECT_NAME { get; set; }
    public string OBJECT_ID { get; set; }
    public DateTime EPOCH { get; set; }
    public float MEAN_MOTION { get; set; }
    public float ECCENTRICITY { get; set; }
    public float INCLINATION { get; set; }
    public float RA_OF_ASC_NODE { get; set; }
    public float ARG_OF_PERICENTER { get; set; }
    public float MEAN_ANOMALY { get; set; }
    public int EPHEMERIS_TYPE { get; set; }
    public string CLASSIFICATION_TYPE { get; set; }
    public int NORAD_CAT_ID { get; set; }
    public int ELEMENT_SET_NO { get; set; }
    public int REV_AT_EPOCH { get; set; }
    public float BSTAR { get; set; }
    public float MEAN_MOTION_DOT { get; set; }
    public float MEAN_MOTION_DDOT { get; set; }
}
