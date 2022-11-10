using System.Data;
using System.Diagnostics;
using System.Globalization;
using ArcaeaUnlimitedAPI.Lib;
using ArcaeaUnlimitedAPI.Lib.Models;
using ArcaeaUnlimitedAPI.Lib.Responses;
using Newtonsoft.Json;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using SQLite;

#pragma warning disable CS8618

namespace SuzuBot.Modules.Arcaea;

internal static class DifficultyColors
{
    public static SKColor Past = new(111, 189, 209);
    public static SKColor Present = new(176, 190, 126);
    public static SKColor Future = new(153, 102, 153);
    public static SKColor Beyond = new(169, 36, 61);
}

internal static class NoteTypeColors
{
    public static SKColor Lost = SKColors.LightGreen;
    public static SKColor Far = new(255, 204, 102);
    public static SKColor Pure = new(51, 153, 255);
    public static SKColor MaxPure = new(204, 102, 204);
}

internal static class ClearTypeColors
{
    public static SKColor Normal = SKColors.White;
    public static SKColor FullRecall = new(133, 205, 80);
    public static SKColor PureMemory = SKColors.DodgerBlue;
    public static SKColor MaxPure = new(228, 152, 57);
}

public enum ChartQueryResultType
{
    Success,
    NotFound,
    Ambiguous
}

internal class ArcaeaUtils
{
    private readonly string _resourceDirPath;
    private readonly AuaClient _client;
    private readonly SQLiteAsyncConnection _songDbConnection;
    private readonly SQLiteAsyncConnection _userDbConnection;
    public SKTypeface NotoSansRegular;
    public SKTypeface SourceHanSansVF;
    public SKTypeface NotoSansCJKscRegular;
    public SKTypeface ExoRegular;
    public SKTypeface ExoSemiBold;
    public SKTypeface GeosansLight;
    public SKTypeface ArkPixel;

    public readonly string[] ClearType =
    {
        "TL",
        "NC",
        "FR",
        "PM",
        "EC",
        "HC"
    };

    public ArcaeaUtils(string resourceDirPath, AuaClient client)
    {
        _client = client;
        _resourceDirPath = resourceDirPath;
        _songDbConnection = new SQLiteAsyncConnection(Path.Combine(resourceDirPath, "ArcaeaSongDatabase", "arcsong.db"));
        _userDbConnection = new SQLiteAsyncConnection(Path.Combine(resourceDirPath, "arcuser.db"));
        NotoSansRegular = SKTypeface.FromFile(Path.Combine(resourceDirPath, "fonts", "NotoSans-Regular.ttf"));
        NotoSansCJKscRegular = SKTypeface.FromFile(Path.Combine(resourceDirPath, "fonts", "NotoSansCJKsc-Regular.otf"));
        SourceHanSansVF = SKTypeface.FromFile(Path.Combine(resourceDirPath, "fonts", "SourceHanSans-VF.otf.ttc"));
        ExoRegular = SKTypeface.FromFile(Path.Combine(resourceDirPath, "fonts", "Exo-Regular.ttf"));
        ExoSemiBold = SKTypeface.FromFile(Path.Combine(resourceDirPath, "fonts", "Exo-SemiBold.ttf"));
        GeosansLight = SKTypeface.FromFile(Path.Combine(resourceDirPath, "fonts", "GeosansLight.ttf"));
        ArkPixel = SKTypeface.FromFile(Path.Combine(resourceDirPath, "fonts", "ark-pixel-12px-monospaced-latin.otf"));
    }

    public (ChartQueryResultType ResultType, string SongId, AuaChartInfo[] ChartInfos) QueryChartPrecise(string songName)
    {
        // Precise match SongId
        var chartQuery = _songDbConnection.Table<ChartInfoMapping>()
            .Where(x => x.SongId == songName)
            .ToArrayAsync().Result;
        if (chartQuery.Any())
            return (ChartQueryResultType.Success, chartQuery[0].SongId, chartQuery.Select(ToAuaChartInfo).ToArray());

        // Precise match SongName
        chartQuery = _songDbConnection.Table<ChartInfoMapping>()
            .Where(x => x.NameEn == songName || x.NameJp == songName)
            .ToArrayAsync().Result;
        if (chartQuery.Any())
            return (ChartQueryResultType.Success, chartQuery[0].SongId, chartQuery.Select(ToAuaChartInfo).ToArray());

        // Precise match Alias
        var aliasResult = _songDbConnection.Table<AliasMapping>()
            .Where(x => x.Alias == songName)
            .ToArrayAsync().Result;
        if (aliasResult.Any())
        {
            var songId = aliasResult[0].SongId;
            if (aliasResult.All(x => x.SongId == songId))
                return QueryChartPrecise(songId);
            else
                return (ChartQueryResultType.Ambiguous,
                        aliasResult[0].SongId,
                    aliasResult
                    .GroupBy(x => x.SongId)
                    .SelectMany(x => QueryChartPrecise(x.Key).ChartInfos).ToArray());
        }

        return (ChartQueryResultType.NotFound, string.Empty, Array.Empty<AuaChartInfo>());
    }
    public (ChartQueryResultType ResultType, string SongId, AuaChartInfo[] ChartInfos) QueryChartCoarse(string songName)
    {
        var preciseResult = QueryChartPrecise(songName);
        if (preciseResult.ResultType == ChartQueryResultType.Success) return preciseResult;

        songName = songName.ToLower();

        // Coarse match SongId
        var songIdResult = _songDbConnection.Table<ChartInfoMapping>()
            .Where(x => x.SongId.Contains(songName))
            .ToArrayAsync().Result;
        if (songIdResult.Any())
        {
            var songId = songIdResult[0].SongId;
            if (songIdResult.All(x => x.SongId == songId))
                return (ChartQueryResultType.Success, songIdResult[0].SongId, songIdResult.Select(ToAuaChartInfo).ToArray());
            else
                return (ChartQueryResultType.Ambiguous, songIdResult[0].SongId, songIdResult.Select(ToAuaChartInfo).ToArray());
        }

        // Coarse match SongName
        var songNameResult = _songDbConnection.Table<ChartInfoMapping>()
            .Where(x => x.NameEn.ToLower().Contains(songName) || x.NameJp.ToLower().Contains(songName))
            .ToArrayAsync().Result;
        if (songNameResult.Any())
        {
            var songId = songNameResult[0].SongId;
            if (songNameResult.All(x => x.SongId == songId))
                return (ChartQueryResultType.Success, songNameResult[0].SongId, songNameResult.Select(ToAuaChartInfo).ToArray());
            else
                return (ChartQueryResultType.Ambiguous, songNameResult[0].SongId, songNameResult.Select(ToAuaChartInfo).ToArray());
        }

        // Coarse match Alias
        var aliasResult = _songDbConnection.Table<AliasMapping>()
            .Where(x => x.Alias.Contains(songName))
            .ToArrayAsync().Result;
        if (aliasResult.Any())
        {
            var songId = aliasResult[0].SongId;
            if (aliasResult.All(x => x.SongId == songId))
                return QueryChartPrecise(songId);
            else
                return (ChartQueryResultType.Ambiguous,
                        aliasResult[0].SongId,
                    aliasResult
                    .GroupBy(x => x.SongId)
                    .SelectMany(x => QueryChartPrecise(x.Key).ChartInfos).ToArray());
        }

        // Api fallback
        try
        {
            var apiResult = _client.Song.Info(songName).Result;
            if (apiResult is null)
                return (ChartQueryResultType.NotFound, string.Empty, Array.Empty<AuaChartInfo>());

            var charts = apiResult.Difficulties;
            return (ChartQueryResultType.Success, apiResult.SongId, charts);
        }
        catch
        {
            return (ChartQueryResultType.NotFound, string.Empty, Array.Empty<AuaChartInfo>());
        }
    }
    public Task<ArcaeaBindInfo?> GetBindInfo(uint userId)
    {
        return _userDbConnection.FindAsync<ArcaeaBindInfo?>(userId);
    }
    public Task<ArcaeaUserInfo?> GetUserInfo(string userCode)
    {
        return _userDbConnection.FindAsync<ArcaeaUserInfo?>(userCode);
    }
    public async Task<ArcaeaUserInfo?> GetUserInfo(uint userId)
    {
        var info = await GetBindInfo(userId);
        if (info is null)
            return null;

        return await GetUserInfo(info.UserCode);
    }
    public bool UpsertBindInfo(ArcaeaBindInfo info)
    {
        return _userDbConnection.InsertOrReplaceAsync(info).Result > 0;
    }
    public bool UpsertUserInfo(ArcaeaUserInfo info)
    {
        return _userDbConnection.InsertOrReplaceAsync(info).Result > 0;
    }
    public bool DeleteBindInfo(uint userId)
    {
        return _userDbConnection.DeleteAsync<ArcaeaUserInfo>(userId).Result > 0;
    }

    public async Task<SKImage> GenerateSongCard(AuaRecord record, int id = 1, bool neko = false)
    {
        // 准备工作
        SKImageInfo imageInfo = new(900, 410);
        using SKSurface surface = SKSurface.Create(imageInfo);
        using SKCanvas mainCanvas = surface.Canvas;
        using SKBitmap cover = SKBitmap.Decode(await GetSongCover(record.SongId, record.Difficulty, neko));
        AuaChartInfo chartInfo = (await GetChartInfo(record.SongId))[record.Difficulty];

        #region 绘制背景
        using (SKPaint bgPaint = new())
        {
            bgPaint.Color = record.ClearType == 3
                ? SKColors.DodgerBlue
                : SKColors.White;
            bgPaint.IsAntialias = true;

            SKRect rect = new(0, 0, 900, 410);
            using SKRoundRect roundRect = new(rect, 25);
            mainCanvas.DrawRoundRect(roundRect, bgPaint);
        }
        #endregion

        #region 绘制封面
        using (SKPaint bgPaint = new())
        {
            bgPaint.IsAntialias = true;

            using SKBitmap scaledCover = new(width: 368, 368);
            cover.ScalePixels(scaledCover, SKFilterQuality.Medium);

            using SKSurface coverSurface = SKSurface.Create(new SKImageInfo(368, 368));
            using SKCanvas coverCanvas = coverSurface.Canvas;

            SKRect clipRect = new(0, 0, 368, 368);
            using SKRoundRect clipRoundRect = new(clipRect, 25);
            coverCanvas.ClipRoundRect(clipRoundRect, antialias: true);
            coverCanvas.DrawBitmap(scaledCover, 0, 0);

            coverSurface.Draw(mainCanvas, 0, 0, null);
        }
        #endregion

        #region 绘制信息板
        using (SKPaint bgPaint = new())
        using (SKPaint blurPaint = new())
        {
            blurPaint.IsAntialias = true;

            bgPaint.Color = new(51, 51, 51, 200);
            bgPaint.IsAntialias = true;

            // 封面背景
            using SKBitmap scaledCover = new(width: 600, 600);
            cover.ScalePixels(scaledCover, SKFilterQuality.Low);

            using SKSurface coverSurface = SKSurface.Create(new SKImageInfo(600, 600));
            using SKCanvas coverCanvas = coverSurface.Canvas;

            SKRect clipRect = new(0, 116, 600, 484);
            using SKRoundRect clipRoundRect = new(clipRect, 25);
            blurPaint.ImageFilter = SKImageFilter.CreateBlur(3f, 3f);
            using SKBitmap clipedCover = new(600, 600);

            using (SKCanvas blurCanvas = new(scaledCover))
            using (SKCanvas clipCanvas = new(clipedCover))
            {
                blurCanvas.DrawBitmap(scaledCover, 0, 0, blurPaint);
                clipCanvas.ClipRoundRect(clipRoundRect, SKClipOperation.Intersect, true);
                clipCanvas.DrawBitmap(scaledCover, 0, 0, bgPaint);
            }
            mainCanvas.DrawBitmap(clipedCover, 300, -116);

            SKRect rect = new(300, 0, 900, 368);
            using SKRoundRect roundRect = new(rect, 25);
            mainCanvas.DrawRoundRect(roundRect, bgPaint);
        }
        #endregion

        #region 绘制定数信息
        using (SKPaint bgPaint = new())
        using (SKPaint fontPaint = new())
        {
            fontPaint.Color = SKColors.White;
            fontPaint.TextAlign = SKTextAlign.Center;
            fontPaint.IsAntialias = true;
            fontPaint.TextSize = 36;
            fontPaint.Typeface = ExoRegular;

            bgPaint.Color = record.Difficulty switch
            {
                0 => DifficultyColors.Past,
                1 => DifficultyColors.Present,
                2 => DifficultyColors.Future,
                3 => DifficultyColors.Beyond,
                _ => DifficultyColors.Future,
            };
            bgPaint.IsAntialias = true;

            SKRect rect = new(308, 8, 600, 58);
            using SKRoundRect roundRect = new(rect, 25);
            mainCanvas.DrawRoundRect(roundRect, bgPaint);

            string ratingStr = $"{record.Rating:00.0000} | {(float)chartInfo.Rating / 10:00.00}";
            fontPaint.MeasureText(ratingStr, ref rect);
            mainCanvas.DrawShapedText(ratingStr, 452, 62 - rect.Height / 2, fontPaint);
        }
        #endregion

        #region 绘制曲名
        using (SKPaint fontPaint = new())
        {
            fontPaint.Color = SKColors.White;
            fontPaint.TextAlign = SKTextAlign.Left;
            fontPaint.IsAntialias = true;
            fontPaint.TextSize = 40;
            fontPaint.Typeface = NotoSansRegular;

            SKRect rect = new();
            fontPaint.MeasureText(chartInfo.NameEn, ref rect);
            mainCanvas.DrawShapedText(chartInfo.NameEn, 320, 115 - rect.Height / 2, fontPaint);
        }
        #endregion

        #region 绘制分数
        using (SKPaint fontPaint = new())
        {
            fontPaint.Color = SKColors.White;
            fontPaint.TextAlign = SKTextAlign.Left;
            fontPaint.IsAntialias = true;
            fontPaint.TextSize = 96;
            fontPaint.Typeface = ExoSemiBold;

            string scoreStr = FormatSongScore(record.Score);
            SKRect rect = new();
            fontPaint.MeasureText(scoreStr, ref rect);
            mainCanvas.DrawShapedText(scoreStr, 320, 220 - rect.Height / 2, fontPaint);
        }
        #endregion

        #region 绘制PFL信息
        using (SKPaint fontPaint = new())
        {
            fontPaint.Color = SKColors.White;
            fontPaint.TextAlign = SKTextAlign.Left;
            fontPaint.IsAntialias = true;
            fontPaint.TextSize = 40;
            fontPaint.Typeface = ExoRegular;

            SKRect rect = new();
            //fontPaint.Color = NoteTypeColors.MaxPure;
            string maxPureStr = $"Pure: {record.PerfectCount} [-{record.PerfectCount - record.ShinyPerfectCount}]";
            fontPaint.MeasureText(maxPureStr, ref rect);
            var offset = rect.Width + 8;
            mainCanvas.DrawShapedText(maxPureStr, 325, 200 + rect.Height, fontPaint);

            //fontPaint.Color = NoteTypeColors.Far;
            string farStr = $"Far: {record.NearCount}";
            fontPaint.MeasureText(farStr, ref rect);
            mainCanvas.DrawShapedText(farStr, 325, 252 + rect.Height, fontPaint);

            //fontPaint.Color = NoteTypeColors.Lost;
            string lostStr = $"Lost: {record.MissCount}";
            fontPaint.MeasureText(lostStr, ref rect);
            mainCanvas.DrawShapedText(lostStr, 325, 304 + rect.Height, fontPaint);
        }
        #endregion

        #region 绘制达成率信息
        using (SKPaint fontPaint = new())
        {
            fontPaint.Color = SKColors.White;
            fontPaint.TextAlign = SKTextAlign.Right;
            fontPaint.IsAntialias = true;
            fontPaint.TextSize = 48;
            fontPaint.Typeface = ExoRegular;

            string achStr = $"Ach: {CalcSongAchievingRate(record, chartInfo) * 100:00.000}%";
            SKRect rect = new();
            fontPaint.MeasureText(achStr, ref rect);
            mainCanvas.DrawShapedText(achStr, 880, 260 + rect.Height, fontPaint);
        }
        #endregion

        #region 绘制游玩时间和序号
        using (SKPaint fontPaint = new())
        {
            fontPaint.Color = SKColors.Black;
            fontPaint.TextAlign = SKTextAlign.Left;
            fontPaint.IsAntialias = true;
            fontPaint.TextSize = 32;
            fontPaint.Typeface = ArkPixel;

            DateTime playTime = DateTime.UnixEpoch.AddMilliseconds(record.TimePlayed);
            string timeStr = playTime.ToString("yyyy-MM-dd HH:mm:ss");
            SKRect rect = new();
            fontPaint.MeasureText(timeStr, ref rect);
            mainCanvas.DrawShapedText(timeStr, 24, 376 + rect.Height, fontPaint);

            fontPaint.TextAlign = SKTextAlign.Right;
            string idStr = $"#{id:00}";
            fontPaint.MeasureText(idStr, ref rect);
            mainCanvas.DrawText(idStr, 880, 376 + rect.Height, fontPaint);
        }
        #endregion

        return surface.Snapshot();
    }
    public async Task<SKImage> GenerateBest30(AuaUserBest30Content best30Content, bool neko = false)
    {
        // 准备工作
        SKImageInfo imageInfo = new(1600, 3400);
        using SKSurface surface = SKSurface.Create(imageInfo);
        using SKCanvas mainCanvas = surface.Canvas;
        var stopWatch = Stopwatch.StartNew();

        #region 绘制背景
        using SKBitmap background = new(2128, 3400);
        using (var originBackground = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(_resourceDirPath, "assets", "world.jpg"))))
        {
            originBackground.ScalePixels(background, SKFilterQuality.Low);
        }

        using (var bitmapCanvas = new SKCanvas(background))
        using (var blurPaint = new SKPaint())
        {
            blurPaint.ImageFilter = SKImageFilter.CreateBlur(10, 10);
            bitmapCanvas.DrawBitmap(background, 0, 0, blurPaint);

            mainCanvas.DrawBitmap(background, 800 - background.Width / 2, 0, null);

        }
        #endregion

        #region 绘制信息板
        using (SKPaint bgPaint = new())
        {
            bgPaint.Color = new(51, green: 51, 51, 191);
            SKRect rect = new(35, 50, 1565, 550);
            using SKRoundRect roundRect = new(rect, 60);
            mainCanvas.DrawRoundRect(roundRect, bgPaint);
        }
        #endregion

        #region 绘制信息板信息
        using (SKPaint bgPaint = new())
        {
            bgPaint.Color = SKColors.White;
            bgPaint.IsAntialias = true;

            using (SKBitmap charaIcon = SKBitmap.Decode(await GetCharaIcon(best30Content.AccountInfo.Character, best30Content.AccountInfo.IsCharUncapped)))
            using (SKBitmap scaledCharaIcon = new(250, 250))
            using (SKBitmap clipCharaIcon = new(250, 250))
            using (var bitmapCanvas = new SKCanvas(clipCharaIcon))
            {
                charaIcon.ScalePixels(scaledCharaIcon, SKFilterQuality.Medium);
                using SKPath clipPath = new();
                clipPath.AddCircle(125, 125, 88);
                bitmapCanvas.ClipPath(clipPath);
                bitmapCanvas.DrawBitmap(scaledCharaIcon, 0, 0);
                mainCanvas.DrawBitmap(clipCharaIcon, 70, 50);
            }

            using (SKBitmap charaIcon = SKBitmap.Decode(GetRatingRankFrame(best30Content.AccountInfo)))
            using (SKBitmap scaledCharaIcon = new(120, 120))
            {
                charaIcon.ScalePixels(scaledCharaIcon, SKFilterQuality.Medium);
                mainCanvas.DrawBitmap(scaledCharaIcon, 210, 177);
            }

            using (var fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.Typeface = NotoSansRegular;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;

                mainCanvas.DrawShapedText(best30Content.AccountInfo.Name, 320, 190, fontPaint);

                fontPaint.TextSize = 48;
                fontPaint.TextAlign = SKTextAlign.Center;
                mainCanvas.DrawShapedText($"< {best30Content.AccountInfo.Code.Insert(6, " ").Insert(3, " ")} >", 525, 260, fontPaint);

                if (best30Content.AccountInfo.Rating < 0)
                {
                    fontPaint.TextSize = 60;
                    fontPaint.Typeface = NotoSansCJKscRegular;
                    fontPaint.TextAlign = SKTextAlign.Center;
                    string YOU = "您";
                    fontPaint.Color = new(80, 73, 89);
                    fontPaint.Style = SKPaintStyle.Stroke;
                    fontPaint.StrokeWidth = 8;
                    mainCanvas.DrawShapedText(YOU, 270, 260, fontPaint);
                    fontPaint.Color = SKColors.White;
                    fontPaint.Style = SKPaintStyle.Fill;
                    mainCanvas.DrawShapedText(YOU, 270, 260, fontPaint);
                }
                else
                {
                    fontPaint.TextSize = 60;
                    fontPaint.Typeface = ExoSemiBold;
                    fontPaint.TextAlign = SKTextAlign.Right;
                    string pttInt = $"{best30Content.AccountInfo.Rating / 100:00}.";

                    fontPaint.Color = new(80, 73, 89);
                    fontPaint.Style = SKPaintStyle.Stroke;
                    fontPaint.StrokeWidth = 8;
                    mainCanvas.DrawShapedText(pttInt, 280, 260, fontPaint);
                    fontPaint.Color = SKColors.White;
                    fontPaint.Style = SKPaintStyle.Fill;
                    mainCanvas.DrawShapedText(pttInt, 280, 260, fontPaint);

                    fontPaint.TextSize = 40;
                    fontPaint.Typeface = ExoSemiBold;
                    fontPaint.TextAlign = SKTextAlign.Left;
                    string pttDec = (best30Content.AccountInfo.Rating % 100).ToString("00");
                    fontPaint.Color = new(80, 73, 89);
                    fontPaint.Style = SKPaintStyle.Stroke;
                    fontPaint.StrokeWidth = 6;
                    mainCanvas.DrawShapedText(pttDec, 284, 260, fontPaint);
                    fontPaint.Color = SKColors.White;
                    fontPaint.Style = SKPaintStyle.Fill;
                    mainCanvas.DrawShapedText(pttDec, 284, 260, fontPaint);
                }
            }

            SKRect rect = new(100, 296, 950, 304);
            using SKRoundRect roundRect = new(rect, 4);
            mainCanvas.DrawRoundRect(roundRect, bgPaint);

            using (var fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.Typeface = NotoSansRegular;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;

                mainCanvas.DrawShapedText($"B30 Average: {best30Content.Best30Avg:00.0000}", 525, 380, fontPaint);
                mainCanvas.DrawShapedText($"R10 Average: {best30Content.Recent10Avg:00.0000}", 525, 450, fontPaint);
                mainCanvas.DrawShapedText($"Max Estimate: {(best30Content.Best30Avg + best30Content.Best30List.Take(10).Sum(x => x.Rating) / 10) / 2:00.0000}", 525, 520, fontPaint);
            }

            using SKBitmap charaImg = SKBitmap.Decode(await GetCharaImg(best30Content.AccountInfo.Character, best30Content.AccountInfo.IsCharUncapped));
            using SKBitmap scaledCharaImg = new(500, 500);
            charaImg.ScalePixels(scaledCharaImg, SKFilterQuality.High);
            mainCanvas.DrawBitmap(scaledCharaImg, 1015, 50);
        }
        #endregion

        #region 绘制B30成绩信息
        List<Action> actions = new();
        for (int i = 0; i < best30Content.Best30List.Length; i++)
        {
            int id = i;
            actions.Add(() =>
            {
                using SKBitmap card = SKBitmap.FromImage(GenerateSongCard(best30Content.Best30List[id], id + 1, neko).Result);
                using SKBitmap scaledCard = new(475, 216);
                card.ScalePixels(scaledCard, SKFilterQuality.High);

                var row = id % 3;
                var col = id / 3;
                mainCanvas.DrawBitmap(scaledCard, 50 + row * 515, 600 + col * 254);
            });
        }

        Parallel.Invoke(actions.ToArray());
        #endregion

        #region 绘制Overflow成绩信息
        if (best30Content.Best30Overflow == null || best30Content.Best30Overflow.Length == 0)
        {
            using var fontPaint = new SKPaint();
            fontPaint.TextAlign = SKTextAlign.Center;
            fontPaint.Typeface = NotoSansRegular;
            fontPaint.TextSize = 128;
            fontPaint.IsAntialias = true;

            fontPaint.Color = fontPaint.Color = new(80, 73, 89);
            fontPaint.Style = SKPaintStyle.Stroke;
            fontPaint.StrokeWidth = 16;
            mainCanvas.DrawShapedText("No Overflow", 800, 3300, fontPaint);
            fontPaint.Color = SKColors.White;
            fontPaint.Style = SKPaintStyle.Fill;
            mainCanvas.DrawShapedText("No Overflow", 800, 3300, fontPaint);
        }
        else
        {
            actions.Clear();
            for (int i = 0; i < best30Content.Best30Overflow.Length; i++)
            {
                int id = i;
                actions.Add(() =>
                {
                    using SKBitmap card = SKBitmap.FromImage(GenerateSongCard(best30Content.Best30Overflow[id], id + 31, neko).Result);
                    using SKBitmap scaledCard = new(475, 216);
                    card.ScalePixels(scaledCard, SKFilterQuality.High);

                    var row = id % 3;
                    var col = id / 3;
                    mainCanvas.DrawBitmap(scaledCard, 50 + row * 515, 3140 + col * 254);
                });
            }

            Parallel.Invoke(actions.ToArray());
        }
        #endregion

        #region 绘制生成信息
        using (var fontPaint = new SKPaint())
        {
            fontPaint.Color = SKColors.White;
            fontPaint.TextAlign = SKTextAlign.Left;
            fontPaint.Typeface = ArkPixel;
            fontPaint.TextSize = 24;
            fontPaint.IsAntialias = true;

            mainCanvas.DrawShapedText($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} Generate By SuzuBot Powered By ArcaeaUnlimitedAPI Cost {stopWatch.Elapsed.TotalMilliseconds} ms", 10, 30, fontPaint);
        }
        #endregion

        return surface.Snapshot();
    }
    public async Task<SKImage> GenerateRecord(AuaRecord record, AuaAccountInfo accountInfo, bool neko = false)
    {
        SKImageInfo imageInfo = new(1400, 800);
        using SKSurface surface = SKSurface.Create(imageInfo);
        using SKCanvas mainCanvas = surface.Canvas;
        using SKBitmap cover = SKBitmap.Decode(await GetSongCover(record.SongId, record.Difficulty, neko));
        AuaChartInfo chartInfo = (await GetChartInfo(record.SongId))[record.Difficulty];

        #region 绘制背景
        using (SKBitmap scaledBitmap = new(1400, 1400))
        using (SKPaint bgPaint = new())
        {
            bgPaint.Color = new(51, 51, 51, 191);

            cover.ScalePixels(scaledBitmap, SKFilterQuality.Low);
            mainCanvas.DrawBitmap(scaledBitmap, 0, -300);
            SKRect rect = new(0, 0, 1400, 800);
            mainCanvas.DrawRect(rect, bgPaint);
        }
        #endregion

        #region 绘制难度信息
        using (SKPaint bgPaint = new())
        using (SKPaint fontPaint = new())
        {
            fontPaint.Color = SKColors.White;
            fontPaint.TextAlign = SKTextAlign.Center;
            fontPaint.IsAntialias = true;
            fontPaint.TextSize = 40;
            fontPaint.Typeface = ExoRegular;

            bgPaint.Color = record.Difficulty switch
            {
                0 => DifficultyColors.Past,
                1 => DifficultyColors.Present,
                2 => DifficultyColors.Future,
                3 => DifficultyColors.Beyond,
                _ => DifficultyColors.Beyond,
            };
            bgPaint.IsAntialias = true;

            SKRect rect = new(20, 20, 220, 70);
            using SKRoundRect roundRect = new(rect, 25);
            mainCanvas.DrawRoundRect(roundRect, bgPaint);

            string difficultyStr = record.Difficulty switch
            {
                0 => "PST ",
                1 => "PRS ",
                2 => "FTR ",
                3 => "BYD ",
                _ => "??? ",
            };
            difficultyStr += GetDifficultyFriendly(chartInfo.Difficulty);
            mainCanvas.DrawShapedText(difficultyStr, 120, 60, fontPaint);

            fontPaint.TextAlign = SKTextAlign.Left;
            mainCanvas.DrawShapedText($"{GetScoreRank(record.Score, chartInfo)} | {ClearType[(int)record.ClearType!]}", 230, 60, fontPaint);
        }
        #endregion

        #region 绘制歌曲信息
        using (SKPaint fontPaint = new())
        {
            fontPaint.Color = SKColors.White;
            fontPaint.TextAlign = SKTextAlign.Left;
            fontPaint.IsAntialias = true;
            fontPaint.TextSize = 64;
            fontPaint.Typeface = SourceHanSansVF;
            fontPaint.FakeBoldText = true;

            mainCanvas.DrawShapedText(chartInfo.NameEn, 30, 150, fontPaint);

            fontPaint.TextSize = 32;
            mainCanvas.DrawShapedText($"Artist: {chartInfo.Artist}", 30, y: 200, fontPaint);
            mainCanvas.DrawShapedText($"Package: {chartInfo.SetFriendly}", 30, y: 240, fontPaint);

            fontPaint.Typeface = ExoSemiBold;
            fontPaint.TextSize = 96;
            mainCanvas.DrawShapedText(FormatSongScore(record.Score), 30, y: 340, fontPaint);

            fontPaint.Typeface = ExoRegular;
            fontPaint.TextSize = 58;
            mainCanvas.DrawShapedText($"Rating: {(float)chartInfo.Rating / 10:0.00} => {record.Rating:0.0000}", 30, y: 410, fontPaint);
        }
        #endregion

        #region 绘制下信息板
        using (SKPaint bgPaint = new())
        {
            bgPaint.Color = record.Difficulty switch
            {
                0 => DifficultyColors.Past,
                1 => DifficultyColors.Present,
                2 => DifficultyColors.Future,
                3 => DifficultyColors.Beyond,
                _ => DifficultyColors.Beyond,
            };
            bgPaint.IsAntialias = true;
            bgPaint.Color = bgPaint.Color.WithAlpha(191);

            SKRect rect = new(0, 500, 1400, 800);
            mainCanvas.DrawRect(rect, bgPaint);
        }
        #endregion

        #region 绘制歌曲信息
        using (SKPaint fontPaint = new())
        {
            fontPaint.Color = SKColors.White;
            fontPaint.TextAlign = SKTextAlign.Left;
            fontPaint.IsAntialias = true;
            fontPaint.TextSize = 40;
            fontPaint.Typeface = ExoRegular;

            mainCanvas.DrawShapedText($"Pure: {record.PerfectCount}[-{record.PerfectCount - record.ShinyPerfectCount}]", 30, 550, fontPaint);
            mainCanvas.DrawShapedText($"Far: {record.NearCount}", 30, 600, fontPaint);
            mainCanvas.DrawShapedText($"Lost: {record.MissCount}", 30, 650, fontPaint);

            mainCanvas.DrawShapedText($"Ach: {CalcSongAchievingRate(record, chartInfo) * 100:00.000}%", 400, 550, fontPaint);

        }
        #endregion

        #region 绘制玩家信息
        using (SKBitmap charaIcon = SKBitmap.Decode(await GetCharaIcon(accountInfo.Character, accountInfo.IsCharUncapped)))
        using (SKBitmap scaledCharaIcon = new(150, 150))
        using (SKBitmap clipCharaIcon = new(150, 150))
        using (var bitmapCanvas = new SKCanvas(clipCharaIcon))
        {
            charaIcon.ScalePixels(scaledCharaIcon, SKFilterQuality.Medium);
            using SKPath clipPath = new();
            clipPath.AddCircle(75, 75, 50);
            bitmapCanvas.ClipPath(clipPath);
            bitmapCanvas.DrawBitmap(scaledCharaIcon, 0, 0);
            mainCanvas.DrawBitmap(clipCharaIcon, 10, 660);
        }

        using (SKBitmap charaIcon = SKBitmap.Decode(GetRatingRankFrame(accountInfo)))
        using (SKBitmap scaledCharaIcon = new(80, 80))
        {
            charaIcon.ScalePixels(scaledCharaIcon, SKFilterQuality.Medium);
            mainCanvas.DrawBitmap(scaledCharaIcon, 87, 730);
        }

        using (var fontPaint = new SKPaint())
        {
            fontPaint.Color = SKColors.White;
            fontPaint.Typeface = NotoSansRegular;
            fontPaint.TextSize = 40;
            fontPaint.IsAntialias = true;

            mainCanvas.DrawShapedText(accountInfo.Name, 150, 720, fontPaint);

            fontPaint.Typeface = GeosansLight;
            fontPaint.TextSize = 24;
            mainCanvas.DrawShapedText($"< {accountInfo.Code.Insert(6, " ").Insert(3, " ")} >", 180, 750, fontPaint);

            fontPaint.Typeface = NotoSansRegular;
            mainCanvas.DrawShapedText($"Date: {DateTime.UnixEpoch.AddMilliseconds(record.TimePlayed):yyyy-MM-dd HH:mm:ss}", 180, 780, fontPaint);

            if (accountInfo.Rating < 0)
            {
                fontPaint.TextSize = 40;
                fontPaint.Typeface = NotoSansCJKscRegular;
                fontPaint.TextAlign = SKTextAlign.Center;
                string YOU = "您";
                fontPaint.Color = new(80, 73, 89);
                fontPaint.Style = SKPaintStyle.Stroke;
                fontPaint.StrokeWidth = 6;
                mainCanvas.DrawShapedText(YOU, 127, 782, fontPaint);
                fontPaint.Color = SKColors.White;
                fontPaint.Style = SKPaintStyle.Fill;
                mainCanvas.DrawShapedText(YOU, 127, 782, fontPaint);
            }
            else
            {
                fontPaint.TextSize = 40;
                fontPaint.Typeface = ExoSemiBold;
                fontPaint.TextAlign = SKTextAlign.Right;
                string pttInt = $"{accountInfo.Rating / 100:00}.";

                fontPaint.Color = new(80, 73, 89);
                fontPaint.Style = SKPaintStyle.Stroke;
                fontPaint.StrokeWidth = 6;
                mainCanvas.DrawShapedText(pttInt, 130, 782, fontPaint);
                fontPaint.Color = SKColors.White;
                fontPaint.Style = SKPaintStyle.Fill;
                mainCanvas.DrawShapedText(pttInt, 130, 782, fontPaint);

                fontPaint.TextSize = 24;
                fontPaint.Typeface = ExoSemiBold;
                fontPaint.TextAlign = SKTextAlign.Left;
                string pttDec = (accountInfo.Rating % 100).ToString("00");
                fontPaint.Color = new(80, 73, 89);
                fontPaint.Style = SKPaintStyle.Stroke;
                fontPaint.StrokeWidth = 6;
                mainCanvas.DrawShapedText(pttDec, 132, 782, fontPaint);
                fontPaint.Color = SKColors.White;
                fontPaint.Style = SKPaintStyle.Fill;
                mainCanvas.DrawShapedText(pttDec, 132, 782, fontPaint);
            }
        }
        #endregion

        #region 绘制搭档立绘
        using (SKBitmap charaImg = SKBitmap.Decode(await GetCharaImg(accountInfo.Character, accountInfo.IsCharUncapped)))
        using (SKBitmap scaledCharaImg = new(800, 800))
        {
            charaImg.ScalePixels(scaledCharaImg, SKFilterQuality.High);
            mainCanvas.DrawBitmap(scaledCharaImg, 750, 0);
        }
        #endregion

        #region 绘制生成信息
        using (var fontPaint = new SKPaint())
        {
            fontPaint.Color = SKColors.White;
            fontPaint.TextAlign = SKTextAlign.Right;
            fontPaint.Typeface = ArkPixel;
            fontPaint.TextSize = 24;
            fontPaint.IsAntialias = true;

            mainCanvas.DrawShapedText($"Generate By SuzuBot Powered By ArcaeaUnlimitedAPI", 1400, 790, fontPaint);
        }
        #endregion

        return surface.Snapshot();
    }

    public static string GetDifficultyFriendly(int difficulty)
    {
        string str = (difficulty / 2).ToString();
        if (difficulty % 2 == 1)
            str += '+';
        return str;
    }
    public AuaChartInfo ToAuaChartInfo(ChartInfoMapping mapping)
    {
        return new AuaChartInfo()
        {
            NameEn = mapping.NameEn,
            NameJp = mapping.NameJp,
            Artist = mapping.Artist,
            Bpm = mapping.Bpm,
            BpmBase = mapping.BpmBase,
            Set = mapping.Set,
            SetFriendly = GetSetFriendlyName(mapping.Set).Result,
            Time = mapping.Time,
            Side = mapping.Side,
            WorldUnlock = mapping.WorldUnlock,
            RemoteDownload = mapping.RemoteDownload,
            Bg = mapping.Bg,
            Date = mapping.Date,
            Version = mapping.Version,
            Difficulty = mapping.Difficulty,
            Rating = mapping.Rating,
            Note = mapping.Note,
            ChartDesigner = mapping.ChartDesigner,
            JacketDesigner = mapping.JacketDesigner,
            JacketOverride = mapping.JacketOverride,
            AudioOverride = mapping.AudioOverride,
        };
    }
    public async Task<AuaChartInfo[]> GetChartInfo(string songId)
    {
        var query = await _songDbConnection.Table<ChartInfoMapping>()
            .Where(x => x.SongId == songId)
            .ToArrayAsync();

        if (query.Length == 0)
        {
            var content = await _client.Song.Info(songId, AuaSongQueryType.SongId);
            return content.Difficulties;
        }
        else
        {
            return query
                .Select(ToAuaChartInfo)
                .ToArray();
        }
    }
    public async Task<string[]> GetSongAlias(string songId)
    {
        var query = await _songDbConnection.Table<AliasMapping>()
            .Where(x => x.SongId == songId)
            .ToArrayAsync();

        if (query.Length <= 0)
        {
            return await _client.Song.Alias(songId, AuaSongQueryType.SongId);
        }
        else
        {
            return query.Select(x => x.Alias).ToArray();
        }
    }
    public async Task<string> GetSetFriendlyName(string set)
    {
        return (await _songDbConnection.FindAsync<PackageMapping>(set))
            ?.Name
            ?? "???";
    }
    public static int GetPlayerRatingRank(AuaAccountInfo accountInfo)
    {
        if (accountInfo.Rating >= 1300)
            return 7;
        else if (accountInfo.Rating >= 1250)
            return 6;
        else if (accountInfo.Rating >= 1200)
            return 5;
        else if (accountInfo.Rating >= 1100)
            return 4;
        else if (accountInfo.Rating >= 1000)
            return 3;
        else if (accountInfo.Rating >= 700)
            return 2;
        else if (accountInfo.Rating >= 350)
            return 1;
        else if (accountInfo.Rating >= 0)
            return 0;
        else
            return 7;
    }
    public byte[] GetRatingRankFrame(AuaAccountInfo accountInfo)
    {
        return File.ReadAllBytes(Path.Combine(_resourceDirPath, "ratings", $"rating_{GetPlayerRatingRank(accountInfo)}.png"));
    }
    public static string GetScoreRank(int score, AuaChartInfo chartInfo)
    {
        if (score == GetMaxScore(chartInfo))
            return "MAX";
        else if (score >= 10_000_000)
            return "PM";
        else if (score >= 09_950_000)
            return "EX+";
        else if (score >= 09_800_000)
            return "EX";
        else if (score >= 09_500_000)
            return "AA";
        else if (score >= 09_200_000)
            return "A";
        else if (score >= 08_900_000)
            return "B";
        else return "D";
    }
    public async Task<byte[]> GetSongCover(string songId, int difficulty, bool neko = false)
    {
        var chartInfo = (await GetChartInfo(songId)).ElementAtOrDefault(difficulty);
        if (chartInfo == null)
            throw new FileNotFoundException();
        var dirPath = Path.Combine(_resourceDirPath, neko ? "nekoCovers" : "covers", chartInfo.RemoteDownload ? $"dl_{songId}" : songId);
        if (Directory.Exists(dirPath))
        {
            var files = Directory.GetFiles(dirPath);
            if (chartInfo.JacketOverride)
            {
                foreach (var file in files)
                {
                    if (Path.GetFileName(file).StartsWith(difficulty.ToString()))
                        return File.ReadAllBytes(file);
                }

                var bytes = await _client.Assets.Song(songId, (ArcaeaDifficulty)difficulty);
                File.WriteAllBytes(Path.Combine(dirPath, $"{difficulty}.png"), bytes);
                return bytes;
            }
            else
            {
                foreach (var file in files)
                {
                    if (Path.GetFileName(file).StartsWith("base"))
                        return File.ReadAllBytes(file);
                }

                var bytes = await _client.Assets.Song(songId, (ArcaeaDifficulty)difficulty);
                File.WriteAllBytes(Path.Combine(dirPath, "base.png"), bytes);
                return bytes;
            }
        }
        else
        {
            Directory.CreateDirectory(dirPath);
            var bytes = await _client.Assets.Song(songId, (ArcaeaDifficulty)difficulty);
            if (chartInfo.JacketOverride)
                File.WriteAllBytes(Path.Combine(dirPath, $"{difficulty}.png"), bytes);
            else
                File.WriteAllBytes(Path.Combine(dirPath, "base.png"), bytes);
            return bytes;
        }
    }
    public async Task<byte[]> GetCharaIcon(int charaId, bool awakened = false)
    {
        var filePath = Path.Combine(_resourceDirPath, "charas", $"{charaId}{(awakened ? "u" : "")}_icon.png");
        if (File.Exists(filePath))
        {
            return File.ReadAllBytes(filePath);
        }
        else
        {
            var bytes = await _client.Assets.Icon(charaId, awakened);
            File.WriteAllBytes(filePath, bytes);
            return bytes;
        }
    }
    public async Task<byte[]> GetCharaImg(int charaId, bool awakened = false)
    {
        var filePath = Path.Combine(_resourceDirPath, "charas", $"{charaId}{(awakened ? "u" : "")}.png");
        if (File.Exists(filePath))
        {
            return File.ReadAllBytes(filePath);
        }
        else
        {
            var bytes = await _client.Assets.Char(charaId, awakened);
            File.WriteAllBytes(filePath, bytes);
            return bytes;
        }
    }
    private static string FormatSongScore(int score)
    {
        return score.ToString().PadLeft(8, '0').Insert(5, "\'").Insert(2, "\'");
    }
    private static int GetMaxScore(AuaChartInfo chartInfo)
    {
        return 10_000_000 + chartInfo.Note;
    }
    private static float CalcSongAchievingRate(AuaRecord record, AuaChartInfo chartInfo)
    {
        if (record.Score >= 10_000_000)
            return 1 + (float)(record.Score - 10_000_000) / chartInfo.Note * 0.01f;
        else
            return (float)record.Score / 10_000_000;
    }
    private static SKColor GetAverageColor(SKBitmap bitmap)
    {
        using var resized = new SKBitmap(1, 1);
        bitmap.ScalePixels(resized, SKFilterQuality.High);
        var color = resized.GetPixel(0, 0);

        return new SKColor((byte)(color.Red / 1.5), (byte)(color.Green / 1.5), (byte)(color.Blue / 1.5));
    }
    private SKColor GetDominantColor(SKBitmap bitmap, int depth = 2)
    {
        using var resized = new SKBitmap(256, 256);
        bitmap.ScalePixels(resized, SKFilterQuality.High);
        var color = resized.GetPixel(0, 0);

        return GetDominantColor(bitmap.Pixels, depth);
    }
    private SKColor GetDominantColor(SKColor[] cube, int depth)
    {
        if (depth == 0)
        {
            byte r = (byte)(cube.Sum(x => x.Red) / cube.Length);
            byte g = (byte)(cube.Sum(x => x.Green) / cube.Length);
            byte b = (byte)(cube.Sum(x => x.Blue) / cube.Length);
            return new SKColor(r, g, b);
        }

        var size = CalcColorCubeSize(cube);

        if (size.Item1 >= size.Item2)
        {
            if (size.Item1 >= size.Item3)
            {
                cube = cube.OrderBy(x => x.Red).Take(cube.Length / 2).ToArray();
            }
            else
            {
                cube = cube.OrderBy(x => x.Blue).Take(cube.Length / 2).ToArray();
            }
        }
        else
        {
            if (size.Item2 >= size.Item3)
            {
                cube = cube.OrderBy(x => x.Green).Take(cube.Length / 2).ToArray();
            }
            else
            {
                cube = cube.OrderBy(x => x.Blue).Take(cube.Length / 2).ToArray();
            }
        }



        return GetDominantColor(cube, depth - 1);
    }
    private static (byte, byte, byte) CalcColorCubeSize(SKColor[] cube)
    {
        cube = cube.OrderBy(x => x.Red).ToArray();
        var redWidth = (byte)(cube.Last().Red - cube.First().Red);
        cube = cube.OrderBy(x => x.Green).ToArray();
        var greenWidth = (byte)(cube.Last().Green - cube.First().Green);
        cube = cube.OrderBy(x => x.Blue).ToArray();
        var blueWidth = (byte)(cube.Last().Blue - cube.First().Blue);

        return (redWidth, greenWidth, blueWidth);
    }
}

[Table("charts")]
internal class ChartInfoMapping
{
    [PrimaryKey]
    [Column("song_id")]
    public string SongId { get; set; }
    [Column("rating_class")]
    public int RatingClass { get; set; }
    [Column("name_en")]
    public string NameEn { get; set; }

    [Column("name_jp")]
    public string NameJp { get; set; }

    [Column("artist")]
    public string Artist { get; set; }

    [Column("bpm")]
    public string Bpm { get; set; }

    [Column("bpm_base")]
    public double BpmBase { get; set; }

    [Column("set")]
    public string Set { get; set; }

    [Column("time")]
    public int Time { get; set; }

    [Column("side")]
    public int Side { get; set; }

    [Column("world_unlock")]
    public bool WorldUnlock { get; set; }

    [Column("remote_download")]
    public bool RemoteDownload { get; set; }

    [Column("bg")]
    public string Bg { get; set; }

    [Column("date")]
    public long Date { get; set; }

    [Column("version")]
    public string Version { get; set; }

    [Column("difficulty")]
    public int Difficulty { get; set; }

    [Column("rating")]
    public int Rating { get; set; }

    [Column("note")]
    public int Note { get; set; }

    [Column("chart_designer")]
    public string ChartDesigner { get; set; }

    [Column("jacket_designer")]
    public string JacketDesigner { get; set; }

    [Column("jacket_override")]
    public bool JacketOverride { get; set; }

    [Column("audio_override")]
    public bool AudioOverride { get; set; }
}

[Table("packages")]
internal class PackageMapping
{
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; }
    [Column("Name")]
    public string Name { get; set; }
}

[Table("alias")]
internal class AliasMapping
{
    [PrimaryKey]
    [Column("alias")]
    public string Alias { get; set; }
    [Column("sid")]
    public string SongId { get; set; }
}

[Table("t_bind_info")]
internal class ArcaeaBindInfo
{
    [PrimaryKey]
    [Column("user_id")]
    public uint UserId { get; set; }
    [Column("user_code")]
    public string UserCode { get; set; }
}

[Table("t_user_info")]
internal class ArcaeaUserInfo
{
    [PrimaryKey]
    [Column("user_code")]
    public string UserCode { get; set; }
    [Column("user_name")]
    public string UserName { get; set; }
    [Column("query_record")]
    public string QueryRecordStr { get; set; } = "[]";
    [Ignore]
    public List<QueryRecord> QueryRecords
    {
        get
        {
            var list = JsonConvert.DeserializeObject<List<string>>(QueryRecordStr) ?? new();
            if (list.Count <= 0) return new();
            List<QueryRecord> result = new();
            foreach (var str in list)
            {
                var record = QueryRecord.Create(str);
                if (record != null)
                {
                    result.Add(record);
                }
            }
            return result;
        }
        set
        {
            var list = value.Select(x => x.ToString()).ToList();
            QueryRecordStr = JsonConvert.SerializeObject(list);
        }
    }
}

internal class QueryRecord
{
    public DateTime DateTime { get; set; }
    public float Potential { get; set; }

    public static QueryRecord? Create(string text)
    {

        var datetime = DateTime.ParseExact(text[..8], "yyyyMMdd", CultureInfo.InvariantCulture);
        if (float.TryParse(text.Substring(8, 4), out var potential))
        {
            potential /= 100;
            return new QueryRecord(datetime, potential);
        }
        else
        {
            return null;
        }
    }

    public QueryRecord(DateTime dateTime, float potential)
    {
        DateTime = dateTime;
        Potential = potential;
    }

    public override string ToString()
    {
        return $"{DateTime:yyyyMMdd}{((int)(Potential * 100)).ToString().PadLeft(4, '0')}";
    }
}