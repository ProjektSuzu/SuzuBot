using System.Text;
using System.Text.Json;
using Konata.Core.Message;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;
using SuzuBot.Modules.Useful.Satellite;

namespace SuzuBot.Modules.Useful;
public class SatelliteModule : BaseModule
{
    private HttpClient _httpClient;
    private const int _issId = 25544;
    private const string _orbitDisplayUrl = @"https://www.heavens-above.com/orbitdisplay.aspx?icon=default&width=512&height=512&satid={satid}";
    private const string _orbitSatidUrl = @"https://celestrak.org/NORAD/elements/gp.php?CATNR={satid}&FORMAT=JSON";
    private const string _orbitNameUrl = @"https://celestrak.org/NORAD/elements/gp.php?NAME={name}&FORMAT=JSON";
    private const string _orbitIdUrl = @"https://celestrak.org/NORAD/elements/gp.php?INTDES={id}&FORMAT=JSON";

    private const string _satcatSatidUrl = @"https://celestrak.org/satcat/records.php?CATNR={satid}&FORMAT=JSON";
    private const string _satcatNameUrl = @"https://celestrak.org/satcat/records.php?NAME={name}&FORMAT=JSON";
    private const string _satcatIdUrl = @"https://celestrak.org/satcat/records.php?INTDES={id}&FORMAT=JSON";

    private static Dictionary<string, string> _sourceCodeDict = JsonSerializer.Deserialize<Dictionary<string, string>>(SATCAT.SOURCE_CODE_DICT_JSON);
    private static Dictionary<string, string> _launchSiteDict = JsonSerializer.Deserialize<Dictionary<string, string>>(SATCAT.LAUNCH_SITE_DICT_JSON);

    public SatelliteModule()
    {
        Name = "卫星观测";
        _httpClient = new()
        {
            Timeout = new TimeSpan(0, 0, 30),
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/110.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");

    }
    [Command("ISS信息", "^iss$")]
    public Task ISS(MessageEventArgs eventArgs, string[] args)
    {
        return OrbitBySatId(eventArgs, new[] { "25544" });
    }
    [Command("卫星轨道", "^orbit ([0-9]+)$")]
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
        var sateInfo = GetSatelliteOrbitData(satid);
        if (sateInfo is null)
        {
            builder.AppendLine($"找不到卫星或卫星不再活跃: {satid}");
            await eventArgs.Reply(new MessageBuilder(builder.ToString()));
            return;
        }

        var sateView = await GetSatelliteView(sateInfo.NORAD_CAT_ID);
        FormatOrbitData(sateInfo, builder);
        await eventArgs.Reply(new MessageBuilder(builder.ToString()).Image(sateView));
    }
    [Command("卫星轨道", "^orbit (.+)$", Priority = 200)]
    public async Task OrbitBySatName(MessageEventArgs eventArgs, string[] args)
    {
        string name = args[0];
        var builder = new StringBuilder("[Satellite]Orbit\n");
        var sateInfo = GetSatelliteOrbitDataByName(name);
        if (sateInfo is null)
        {
            builder.AppendLine($"找不到卫星或卫星不再活跃: {name}");
            await eventArgs.Reply(new MessageBuilder(builder.ToString()));
            return;
        }

        var sateView = await GetSatelliteView(sateInfo.NORAD_CAT_ID);
        FormatOrbitData(sateInfo, builder);
        await eventArgs.Reply(new MessageBuilder(builder.ToString()).Image(sateView));
    }
    [Command("卫星轨道", "^orbit ([0-9]{4}-.+)$")]
    public async Task OrbitByIntdes(MessageEventArgs eventArgs, string[] args)
    {
        string intdes = args[0];
        var builder = new StringBuilder("[Satellite]Orbit\n");
        var sateInfo = GetSatelliteOrbitDataByIntdes(intdes);
        if (sateInfo is null)
        {
            builder.AppendLine($"找不到卫星或卫星不再活跃: {intdes}");
            await eventArgs.Reply(new MessageBuilder(builder.ToString()));
            return;
        }

        var sateView = await GetSatelliteView(sateInfo.NORAD_CAT_ID);
        FormatOrbitData(sateInfo, builder);
        await eventArgs.Reply(new MessageBuilder(builder.ToString()).Image(sateView));
    }
    public void FormatOrbitData(SatelliteOrbitData sateInfo, StringBuilder builder)
    {

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
    }
    [Command("卫星信息", "^satellite ([0-9]+)$")]
    public async Task SATCATBySatId(MessageEventArgs eventArgs, string[] args)
    {
        int satid;
        var builder = new StringBuilder("[Satellite]SATCAT\n");
        if (!int.TryParse(args[0], out satid))
        {
            builder.AppendLine($"空间飞行器目录编号格式错误: {args[0]}");
            await eventArgs.Reply(new MessageBuilder(builder.ToString()));
            return;
        }
        var sateInfo = GetSatelliteCatalogData(satid);
        if (sateInfo is null)
        {
            builder.AppendLine($"找不到卫星或卫星不再活跃: {satid}");
            await eventArgs.Reply(new MessageBuilder(builder.ToString()));
            return;
        }

        FormatCatalogData(sateInfo, builder);
        await eventArgs.Reply(new MessageBuilder(builder.ToString()));
    }
    [Command("卫星信息", "^satellite (.+)$", Priority = 200)]
    public async Task SATCATBySatName(MessageEventArgs eventArgs, string[] args)
    {
        string name = args[0];
        var builder = new StringBuilder("[Satellite]SATCAT\n");
        var sateInfo = GetSatelliteCatalogDataByName(name);
        if (sateInfo is null)
        {
            builder.AppendLine($"找不到卫星或卫星不再活跃: {name}");
            await eventArgs.Reply(new MessageBuilder(builder.ToString()));
            return;
        }

        FormatCatalogData(sateInfo, builder);
        await eventArgs.Reply(new MessageBuilder(builder.ToString()));
    }
    [Command("卫星信息", "^satellite ([0-9]{4}-.+)$")]
    public async Task SATCATById(MessageEventArgs eventArgs, string[] args)
    {
        string id = args[0];
        var builder = new StringBuilder("[Satellite]SATCAT\n");
        var sateInfo = GetSatelliteCatalogDataByIntdes(id);
        if (sateInfo is null)
        {
            builder.AppendLine($"找不到卫星或卫星不再活跃: {id}");
            await eventArgs.Reply(new MessageBuilder(builder.ToString()));
            return;
        }

        FormatCatalogData(sateInfo, builder);
        await eventArgs.Reply(new MessageBuilder(builder.ToString()));
    }
    private void FormatCatalogData(SatelliteCatalogData sateInfo, StringBuilder builder)
    {
        builder.AppendLine($"空间飞行器目录编号\t{sateInfo.NORAD_CAT_ID}");
        builder.AppendLine($"空间飞行器目录名称\t{sateInfo.OBJECT_NAME}");
        builder.AppendLine($"国际卫星标识符\t{sateInfo.OBJECT_ID}");
        var sateType = sateInfo.OBJECT_TYPE switch
        {
            "PAY" => "载荷",
            "R/B" => "箭体",
            "DEB" => "残骸",
            "UNK" => "未知",
        };
        builder.AppendLine($"卫星类别\t{sateType}");
        var sateStatus = sateInfo.OPS_STATUS_CODE switch
        {
            "+" => "运作",
            "-" => "不在运作",
            "P" => "部分运作",
            "B" => "备份/待命",
            "S" => "空闲",
            "X" => "额外任务",
            "D" => "坠毁",
            "?" => "未知",
        };
        builder.AppendLine($"卫星状态\t{sateStatus}");
        builder.AppendLine($"国家/组织\t{_sourceCodeDict[sateInfo.OWNER]}");
        builder.AppendLine();
        builder.AppendLine($"发射日期\t{sateInfo.LAUNCH_DATE}");
        builder.AppendLine($"发射地点\t{_launchSiteDict[sateInfo.LAUNCH_SITE]}");
        if (sateInfo.DECAY_DATE != string.Empty)
        {
            builder.AppendLine($"坠毁日期\t{sateInfo.DECAY_DATE}");
        }
        if (sateInfo.PERIOD is not null)
        {
            builder.AppendLine($"轨道周期\t{sateInfo.PERIOD} min");
        }
        if (sateInfo.INCLINATION is not null)
        {
            builder.AppendLine($"轨道倾角\t{sateInfo.INCLINATION}");
        }
        if (sateInfo.APOGEE is not null)
        {
            builder.AppendLine($"远地点\t{sateInfo.APOGEE} km");
        }
        if (sateInfo.PERIGEE is not null)
        {
            builder.AppendLine(handler: $"近地点\t{sateInfo.PERIGEE} km");
        }
        if (sateInfo.RCS is not null)
        {
            builder.AppendLine(handler: $"雷达截面\t{sateInfo.RCS} ㎡");
        }
        if (sateInfo.DATA_STATUS_CODE != string.Empty)
        {
            var dataStatus = sateInfo.DATA_STATUS_CODE switch
            {
                "NCE" => "无当前元素",
                "NIE" => "无初始元素",
                "NEA" => "无可用元素",
            };
            builder.AppendLine(handler: $"数据状态码\t{dataStatus}");
        }
        var orbitCenter = sateInfo.ORBIT_CENTER switch
        {
            "AS" => "小行星",
            "CO" => "彗星",
            "EA" => "地球",
            "EL1" => "地球拉格朗日点L1",
            "EL2" => "地球拉格朗日点L2",
            "EL3" => "地球拉格朗日点L3",
            "EL4" => "地球拉格朗日点L4",
            "EL5" => "地球拉格朗日点L5",
            "EM" => "地月质心",
            "JU" => "木星",
            "MA" => "火星",
            "ME" => "水星",
            "MO" => "月球",
            "NE" => "海王星",
            "PL" => "冥王星",
            "SA" => "土星",
            "SS" => "脱离太阳系",
            "SU" => "太阳",
            "UR" => "天王星",
            "VE" => "金星",
            _ => sateInfo.ORBIT_CENTER
        };
        if (int.TryParse(orbitCenter, out var centerId))
        {
            var info = GetSatelliteCatalogData(centerId);
            orbitCenter = info.OBJECT_NAME;
        }
        builder.AppendLine(handler: $"轨道中心\t{orbitCenter}");
        var orbitType = sateInfo.ORBIT_TYPE switch
        {
            "ORB" => "环绕",
            "LAD" => "着陆",
            "IMP" => "撞击",
            "DOC" => "对接",
            "R/T" => "往返",
        };
        builder.AppendLine(handler: $"轨道类型\t{orbitType}");
    }
    private Task<byte[]> GetSatelliteView(int satid)
    {
        var url = _orbitDisplayUrl.Replace("{satid}", satid.ToString());
        if (satid == _issId)
            url = url.Replace("icon=default", "icon=iss");
        return _httpClient.GetByteArrayAsync(url);
    }
    private SatelliteOrbitData? GetSatelliteOrbitData(int satid)
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
    private SatelliteOrbitData? GetSatelliteOrbitDataByName(string name)
    {
        name = name.ToUpper();
        var url = _orbitNameUrl.Replace("{name}", name);
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
    private SatelliteOrbitData? GetSatelliteOrbitDataByIntdes(string id)
    {
        id = id.ToUpper();
        var url = _orbitIdUrl.Replace("{id}", id);
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
    private SatelliteCatalogData? GetSatelliteCatalogData(int satid)
    {
        var url = _satcatSatidUrl.Replace("{satid}", satid.ToString());
        var result = _httpClient.GetStringAsync(url).Result;
        try
        {
            var list = JsonSerializer.Deserialize<SatelliteCatalogData[]>(result);
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
    private SatelliteCatalogData? GetSatelliteCatalogDataByName(string name)
    {
        name = name.ToUpper();
        var url = _satcatNameUrl.Replace("{name}", name);
        var result = _httpClient.GetStringAsync(url).Result;
        try
        {
            var list = JsonSerializer.Deserialize<SatelliteCatalogData[]>(result);
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
    private SatelliteCatalogData? GetSatelliteCatalogDataByIntdes(string id)
    {
        id = id.ToUpper();
        var url = _satcatIdUrl.Replace("{id}", id);
        var result = _httpClient.GetStringAsync(url).Result;
        try
        {
            var list = JsonSerializer.Deserialize<SatelliteCatalogData[]>(result);
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
public record SatelliteCatalogData
{
    public string OBJECT_NAME { get; set; }
    public string OBJECT_ID { get; set; }
    public int NORAD_CAT_ID { get; set; }
    public string OBJECT_TYPE { get; set; }
    public string OPS_STATUS_CODE { get; set; }
    public string OWNER { get; set; }
    public string LAUNCH_DATE { get; set; }
    public string LAUNCH_SITE { get; set; }
    public string DECAY_DATE { get; set; }
    public float? PERIOD { get; set; }
    public float? INCLINATION { get; set; }
    public int? APOGEE { get; set; }
    public int? PERIGEE { get; set; }
    public float? RCS { get; set; }
    public string DATA_STATUS_CODE { get; set; }
    public string ORBIT_CENTER { get; set; }
    public string ORBIT_TYPE { get; set; }
}
