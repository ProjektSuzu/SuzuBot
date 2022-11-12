using System.Text;
using ArcaeaUnlimitedAPI.Lib;
using ArcaeaUnlimitedAPI.Lib.Models;
using ArcaeaUnlimitedAPI.Lib.Responses;
using ArcaeaUnlimitedAPI.Lib.Utils;
using Konata.Core.Message;
using Newtonsoft.Json;
using SuzuBot.Common;
using SuzuBot.Common.Attributes;
using SuzuBot.Common.EventArgs.Messages;

#pragma warning disable CS8618

namespace SuzuBot.Modules.Arcaea;

[Module("Arcaea")]
internal class ArcaeaModule : BaseModule
{
    ArcaeaUtils _utils;
    AuaClient _client;
    Dictionary<int, AuaStatus> _status;

    public override void Init()
    {
        base.Init();
        AuaAuth auaAuth = JsonConvert.DeserializeObject<AuaAuth>(File.ReadAllText(Path.Combine(ResourceDirPath, "auth.json")));
        if (auaAuth is null)
            throw new FileNotFoundException(Path.Combine(ResourceDirPath, "auth.json"));

        _status = JsonConvert.DeserializeObject<Dictionary<int, AuaStatus>>(File.ReadAllText(Path.Combine(ResourceDirPath, "status.json")));
        if (auaAuth is null)
            throw new FileNotFoundException(Path.Combine(ResourceDirPath, "status.json"));

        _client = new()
        {
            ApiUrl = auaAuth.Url,
            Token = auaAuth.Token,
            UserAgent = "114514"
        };
        _client.Initialize();
        _utils = new(ResourceDirPath, _client);
    }

    [Command("Arcaea", "arc", "arcaea", MatchType = Common.Attributes.MatchType.StartsWith)]
    [Command("Arcaea", "a", MatchType = Common.Attributes.MatchType.StartsWith, Priority = 128)]
    public Task Arcaea(MessageEventArgs eventArgs, string[] args, bool neko = false)
    {
        if (!args.Any()) return Recent(eventArgs, neko);

        string funcName = args[0];
        args = args[1..];
        string arguments = string.Join(' ', args.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

        switch (funcName)
        {
            case "alias":
                {
                    return Alias(eventArgs, arguments);
                }
            case "best30":
            case "b30":
                {
                    return Best30(eventArgs, arguments, neko);
                }
            case "best":
            case "info":
                {
                    return Best(eventArgs, arguments, neko);
                }
            case "preview":
            case "chart":
                {
                    return ChartPreview(eventArgs, arguments);
                }
            case "songinfo":
            case "song":
                {
                    return SongInfo(eventArgs, arguments, neko);
                }
            case "bind":
                {
                    return Bind(eventArgs, arguments);
                }
            case "unbind":
                {
                    return Unbind(eventArgs);
                }
            case "recent":
            case "r":
                {
                    return Recent(eventArgs, neko);
                }

            default:
                {
                    StringBuilder builder = new();
                    builder.AppendLine("[Arcaea]Error");
                    builder.AppendLine("∑(O_O；)找不到功能");
                    builder.AppendLine(funcName);
                    return eventArgs.Reply(new MessageBuilder(builder.ToString()));
                }
        }
    }

    [Command("Arcnya", "arcnya", MatchType = Common.Attributes.MatchType.StartsWith, Priority = 126)]
    public Task Arcnya(MessageEventArgs eventArgs, string[] args)
    {
        return Arcaea(eventArgs, args, true);
    }

    public async Task Recent(MessageEventArgs eventArgs, bool neko = false)
    {
        var info = await _utils.GetUserInfo(eventArgs.SenderId);
        if (info is null)
        {
            await NotBindReply(eventArgs);
            return;
        }

        AuaUserInfoContent recent;
        try
        {
            recent = await _client.User.Info(int.Parse(info.UserCode), 1, AuaReplyWith.Recent);
        }
        catch (AuaException ex)
        {
            await ApiErrorReply(eventArgs, ex);
            return;
        }

        using var image = await _utils.GenerateRecord(recent.RecentScore[0], recent.AccountInfo, neko);
        var bytes = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 100).ToArray();
        await eventArgs.Reply(new MessageBuilder("[Arcaea]Recent\n").Image(bytes));
    }
    public async Task Best(MessageEventArgs eventArgs, string queryStr, bool neko = false)
    {
        var info = await _utils.GetUserInfo(eventArgs.SenderId);
        if (info is null)
        {
            await NotBindReply(eventArgs);
            return;
        }

        if (string.IsNullOrWhiteSpace(queryStr))
        {
            await EmptyQueryString(eventArgs);
            return;
        }

        var (SongName, Difficulty) = ParseSongQueryRequest(queryStr);
        var (ResultType, SongId, ChartInfos) = _utils.QueryChartCoarse(SongName);
        switch (ResultType)
        {
            case ChartQueryResultType.Success:
                {
                    if (ChartInfos.Length < Difficulty)
                    {
                        await InvalidChartDifficulty(eventArgs, ChartInfos, Difficulty);
                        return;
                    }

                    break;
                }
            case ChartQueryResultType.NotFound:
                {
                    await SongNotFound(eventArgs, SongName);
                    return;
                }
            case ChartQueryResultType.Ambiguous:
                {
                    await AmbiguousSongResult(eventArgs, SongName, ChartInfos);
                    return;
                }
        }

        AuaUserBestContent best;
        try
        {
            best = await _client.User.Best(int.Parse(info.UserCode), SongId, (ArcaeaDifficulty)Difficulty);
        }
        catch (AuaException ex)
        {
            await ApiErrorReply(eventArgs, ex);
            return;
        }

        using var image = await _utils.GenerateRecord(best.Record, best.AccountInfo, neko);
        var bytes = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 100).ToArray();
        await eventArgs.Reply(new MessageBuilder("[Arcaea]Best\n").Image(bytes));
    }
    public async Task Best30(MessageEventArgs eventArgs, string userCode = "", bool neko = false)
    {
        int code;
        if (string.IsNullOrWhiteSpace(userCode))
        {
            var info = await _utils.GetBindInfo(eventArgs.SenderId);
            if (info is null)
            {
                await NotBindReply(eventArgs);
                return;
            }

            code = int.Parse(info.UserCode);
        }

        AuaUserBest30Content best30;
        try
        {
            best30 = await _client.User.Best30(userCode, 3);
        }
        catch (AuaException ex)
        {
            await ApiErrorReply(eventArgs, ex);
            return;
        }

        using var image = await _utils.GenerateBest30(best30, neko);
        var bytes = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 80).ToArray();
        await eventArgs.Reply(new MessageBuilder("[Arcaea]Best30\n").Image(bytes));
    }
    public async Task SongInfo(MessageEventArgs eventArgs, string queryStr, bool neko = false)
    {
        if (string.IsNullOrWhiteSpace(queryStr))
        {
            await EmptyQueryString(eventArgs);
            return;
        }

        var (SongName, Difficulty) = ParseSongQueryRequest(queryStr);
        var (ResultType, SongId, ChartInfos) = _utils.QueryChartCoarse(SongName);
        switch (ResultType)
        {
            case ChartQueryResultType.Success:
                {
                    if (ChartInfos.Length < Difficulty)
                    {
                        await InvalidChartDifficulty(eventArgs, ChartInfos, Difficulty);
                        return;
                    }

                    break;
                }
            case ChartQueryResultType.NotFound:
                {
                    await SongNotFound(eventArgs, SongName);
                    return;
                }
            case ChartQueryResultType.Ambiguous:
                {
                    await AmbiguousSongResult(eventArgs, SongName, ChartInfos);
                    return;
                }
        }

        var chart = ChartInfos[0];
        var nameEns = ChartInfos.Select(x => x.NameEn.Trim()).Distinct();
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var name in nameEns)
            stringBuilder.AppendLine(name);
        if (!string.IsNullOrWhiteSpace(chart.NameJp))
            stringBuilder.AppendLine(chart.NameJp);
        stringBuilder.AppendLine($"曲包: {chart.SetFriendly}");
        switch (chart.Side)
        {
            case 0:
                stringBuilder.AppendLine("光芒之侧"); break;
            case 1:
                stringBuilder.AppendLine("纷争之侧"); break;
            case 2:
                stringBuilder.AppendLine("消色之侧"); break;
            default:
                stringBuilder.AppendLine("未知之侧"); break;
        }
        stringBuilder.AppendLine("难度    定数    物量");
        stringBuilder.AppendLine(string.Format("PST {0,-4}{1,-8:0.00}{2,-8}", ArcaeaUtils.GetDifficultyFriendly(ChartInfos[0].Difficulty), (float)ChartInfos[0].Rating / 10, ChartInfos[0].Note));
        stringBuilder.AppendLine(string.Format("PRS {0,-4}{1,-8:0.00}{2,-8}", ArcaeaUtils.GetDifficultyFriendly(ChartInfos[1].Difficulty), (float)ChartInfos[1].Rating / 10, ChartInfos[1].Note));
        stringBuilder.AppendLine(string.Format("FTR {0,-4}{1,-8:0.00}{2,-8}", ArcaeaUtils.GetDifficultyFriendly(ChartInfos[2].Difficulty), (float)ChartInfos[2].Rating / 10, ChartInfos[2].Note));
        if (ChartInfos.Length > 3)
            stringBuilder.AppendLine(string.Format("BYD {0,-4}{1,-8:0.00}{2,-8}", ArcaeaUtils.GetDifficultyFriendly(ChartInfos[3].Difficulty), (float)ChartInfos[3].Rating / 10, ChartInfos[3].Note));

        MessageBuilder builder = new MessageBuilder("[Arcaea]SongInfo\n");
        int count = 0;
        var coverQuery = ChartInfos
            .Select(x => (count++, x.JacketOverride)).ToArray();
        byte[][] coverResult;
        if (coverQuery.All(x => x.JacketOverride == false))
        {
            coverResult = new byte[1][] { _utils.GetSongCover(SongId, 0, neko).Result };
        }
        else
        {
            var baseCover = coverQuery.First(x => !x.JacketOverride);
            coverQuery = coverQuery.Where(x => x.JacketOverride).Append(baseCover).OrderBy(x => x.Item1).ToArray();
            coverResult = coverQuery.Select(x => _utils.GetSongCover(SongId, x.Item1, neko).Result).ToArray();
        }

        foreach (var image in coverResult)
            builder.Image(image);

        builder.Text(stringBuilder.ToString());
        await eventArgs.Reply(builder);
    }
    public async Task Alias(MessageEventArgs eventArgs, string queryStr)
    {
        if (string.IsNullOrWhiteSpace(queryStr))
        {
            await EmptyQueryString(eventArgs);
            return;
        }

        var (SongName, Difficulty) = ParseSongQueryRequest(queryStr, false);
        var (ResultType, SongId, ChartInfos) = _utils.QueryChartCoarse(SongName);
        switch (ResultType)
        {
            case ChartQueryResultType.Success:
                {
                    if (ChartInfos.Length < Difficulty)
                    {
                        await InvalidChartDifficulty(eventArgs, ChartInfos, Difficulty);
                        return;
                    }

                    break;
                }
            case ChartQueryResultType.NotFound:
                {
                    await SongNotFound(eventArgs, SongName);
                    return;
                }
            case ChartQueryResultType.Ambiguous:
                {
                    await AmbiguousSongResult(eventArgs, SongName, ChartInfos);
                    return;
                }
        }

        var alias = await _utils.GetSongAlias(SongId);
        StringBuilder builder = new StringBuilder("[Arcaea]Alias\n");
        builder.AppendLine($"{queryStr} 具有以下别名");
        foreach (var alia in alias)
            builder.AppendLine(alia);

        await eventArgs.Reply(builder.ToString());
    }
    public async Task ChartPreview(MessageEventArgs eventArgs, string queryStr)
    {
        if (string.IsNullOrWhiteSpace(queryStr))
        {
            await EmptyQueryString(eventArgs);
            return;
        }

        var (SongName, Difficulty) = ParseSongQueryRequest(queryStr);
        var (ResultType, SongId, ChartInfos) = _utils.QueryChartCoarse(SongName);
        switch (ResultType)
        {
            case ChartQueryResultType.Success:
                {
                    if (ChartInfos.Length < Difficulty)
                    {
                        await InvalidChartDifficulty(eventArgs, ChartInfos, Difficulty);
                        return;
                    }

                    break;
                }
            case ChartQueryResultType.NotFound:
                {
                    await SongNotFound(eventArgs, SongName);
                    return;
                }
            case ChartQueryResultType.Ambiguous:
                {
                    await AmbiguousSongResult(eventArgs, SongName, ChartInfos);
                    return;
                }
        }

        var bytes = await _client.Assets.Preview(SongId, (ArcaeaDifficulty)Difficulty);
        await eventArgs.Reply(new MessageBuilder($"[Arcaea]Preview\n{ChartInfos[Difficulty].NameEn} - {(ArcaeaDifficulty)Difficulty}\n").Image(bytes));
    }
    public async Task Bind(MessageEventArgs eventArgs, string userCode)
    {
        int code;
        AuaUserInfoContent userInfoResult;
        if (TryParseUserCode(userCode, out code))
        {
            try
            {
                userInfoResult = await _client.User.Info(code);
            }
            catch (AuaException ex)
            {
                await ApiErrorReply(eventArgs, ex);
                return;
            }
        }
        else
        {
            try
            {
                userInfoResult = await _client.User.Info(userCode);
            }
            catch (AuaException ex)
            {
                await ApiErrorReply(eventArgs, ex);
                return;
            }
        }

        StringBuilder builder = new("[Arcaea]Bind\n");
        var bindInfo = await _utils.GetBindInfo(eventArgs.SenderId);
        if (bindInfo is null)
        {
            bindInfo = new()
            {
                UserId = eventArgs.SenderId,
                UserCode = userInfoResult.AccountInfo.Code
            };
            var userInfo = await _utils.GetUserInfo(bindInfo.UserCode);
            if (userInfo is null)
            {
                userInfo = new()
                {
                    UserCode = userInfoResult.AccountInfo.Code,
                    UserName = userInfoResult.AccountInfo.Name,
                    QueryRecords = new()
                };
                _utils.UpsertUserInfo(userInfo);
            }

            _utils.UpsertBindInfo(bindInfo);
            builder.AppendLine($"已绑定到 {userInfoResult.AccountInfo.Name}({userInfoResult.AccountInfo.Code})");
        }
        else
        {
            bindInfo.UserCode = userInfoResult.AccountInfo.Code;
            var userInfo = await _utils.GetUserInfo(bindInfo.UserCode);
            if (userInfo is null)
            {
                userInfo = new()
                {
                    UserCode = userInfoResult.AccountInfo.Code,
                    UserName = userInfoResult.AccountInfo.Name,
                    QueryRecords = new()
                };
                _utils.UpsertUserInfo(userInfo);
            }

            _utils.UpsertBindInfo(bindInfo);
            builder.AppendLine($"{userInfo.UserName}({userInfo.UserCode})");
            builder.AppendLine($"已更换绑定到");
            builder.AppendLine($"{userInfoResult.AccountInfo.Name}({userInfoResult.AccountInfo.Code})");
        }

        await eventArgs.Reply(new MessageBuilder(builder.ToString()));
    }
    public async Task Unbind(MessageEventArgs eventArgs)
    {
        var result = _utils.DeleteBindInfo(eventArgs.SenderId);
        StringBuilder builder = new("[Arcaea]Unbind\n");
        if (result)
        {
            builder.AppendLine("已解绑");
        }
        else
        {
            builder.AppendLine("当前用户没有绑定记录");
        }

        await eventArgs.Reply(new MessageBuilder(builder.ToString()));
    }

    public Task ApiErrorReply(MessageEventArgs eventArgs, AuaException ex)
    {
        StringBuilder builder = new();
        builder.AppendLine("[Arcaea]Error");
        builder.AppendLine("∑(O_O；)远端服务器返回了一个错误");
        builder.AppendLine($"{ex.Status}: {_status[ex.Status].Message}");
        return eventArgs.Reply(new MessageBuilder(builder.ToString()));
    }
    public static Task WrongUserCode(MessageEventArgs eventArgs, string userCode)
    {
        StringBuilder builder = new();
        builder.AppendLine("[Arcaea]Error");
        builder.AppendLine("∑(O_O；)无法识别的好友代码");
        builder.AppendLine(userCode);
        builder.AppendLine("请确认格式是否正确");
        return eventArgs.Reply(new MessageBuilder(builder.ToString()));
    }
    public static Task NotBindReply(MessageEventArgs eventArgs)
    {
        StringBuilder builder = new();
        builder.AppendLine("[Arcaea]Error");
        builder.AppendLine("∑(O_O；)未能找到你的绑定记录");
        builder.AppendLine("请先使用");
        builder.AppendLine("/arc bind 好友代码或用户名");
        builder.AppendLine("进行绑定");
        return eventArgs.Reply(new MessageBuilder(builder.ToString()));
    }
    public static Task EmptyQueryString(MessageEventArgs eventArgs)
    {
        StringBuilder builder = new();
        builder.AppendLine("[Arcaea]Error");
        builder.AppendLine("∑(O_O；)搜索条件为空");
        return eventArgs.Reply(new MessageBuilder(builder.ToString()));
    }
    public static Task InvalidChartDifficulty(MessageEventArgs eventArgs, AuaChartInfo[] chartInfos, int difficulty)
    {
        StringBuilder builder = new();
        builder.AppendLine("[Arcaea]Error");
        builder.AppendLine("∑(O_O；)目标歌曲不存在所指定的难度的铺面");
        builder.Append($"{chartInfos[0].NameEn} - ");
        switch (difficulty)
        {
            case 3: builder.AppendLine("Beyond"); break;
            // Nearly inpossible, but who knows.
            case 2: builder.AppendLine("Future"); break;
            case 1: builder.AppendLine("Present"); break;
            case 0: builder.AppendLine("Past"); break;
        }
        return eventArgs.Reply(new MessageBuilder(builder.ToString()));
    }
    public static Task AmbiguousSongResult(MessageEventArgs eventArgs, string songName, AuaChartInfo[] chartInfos)
    {
        var charts = chartInfos
            .Select(x => x.NameEn)
            .Distinct().ToArray();
        StringBuilder builder = new();
        builder.AppendLine("[Arcaea]Error");
        builder.AppendLine("∑(O_O；)搜索条件匹配多个结果");
        builder.AppendLine($"{songName} 同时满足以下歌曲");
        foreach (var chart in charts.Take(5))
            builder.AppendLine(chart);
        if (charts.Length > 5)
            builder.AppendLine($"等 {charts.Length} 个歌曲");
        builder.AppendLine("请缩小搜索范围");
        return eventArgs.Reply(new MessageBuilder(builder.ToString()));
    }
    public static Task SongNotFound(MessageEventArgs eventArgs, string songName)
    {
        StringBuilder builder = new();
        builder.AppendLine("[Arcaea]Error");
        builder.AppendLine("∑(O_O；)找不到指定歌曲");
        builder.AppendLine(songName);
        return eventArgs.Reply(new MessageBuilder(builder.ToString()));
    }
    public static bool TryParseUserCode(string userCode, out int code)
    {
        return int.TryParse(userCode, out code) && userCode.Length == 9;
    }
    public static (string SongName, int Difficulty) ParseSongQueryRequest(string queryStr, bool processDifficuly = true)
    {
        if (!processDifficuly)
            return (queryStr, 2);

        var array = queryStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var songName = string.Join(' ', array.SkipLast(1));
        switch (array.Last().ToLower())
        {
            case "byd":
            case "beyond":
            case "3":
                return (songName, 3);

            case "ftr":
            case "future":
            case "2":
                return (songName, 2);

            case "prs":
            case "present":
            case "1":
                return (songName, 1);

            case "pst":
            case "past":
            case "0":
                return (songName, 0);

            default:
                return (queryStr, 2);
        }
    }
}

internal class AuaAuth
{
    public string Url { get; set; }
    public string Token { get; set; }
}

internal class AuaStatus
{
    public string Description { get; set; }
    public string Message { get; set; }
}