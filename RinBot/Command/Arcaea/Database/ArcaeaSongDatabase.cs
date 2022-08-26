using RinBot.Core;
using SQLite;

namespace RinBot.Command.Arcaea.Database
{
    internal class ArcaeaSongDatabase
    {
        public ArcaeaSongDatabase()
        {
            dbConnection = new(ARCAEA_SONG_DB_PATH);
        }
        private static string ARCAEA_SONG_DB_PATH => Path.Combine(ArcaeaModule.DATABASE_DIR_PATH, "arcsong.db");
        private SQLiteAsyncConnection dbConnection;
        public SQLiteAsyncConnection DBConnection
            => dbConnection;

        public async Task<Chart?> GetChart(string songId, RatingClass ratingClass)
        {
            return await dbConnection.Table<Chart>()
                .Where(x => x.SongId == songId && x.RatingClass == ratingClass)
                .FirstOrDefaultAsync();
        }
        public async Task<List<Chart>> GetChartsPrecise(string songId)
        {
            return await dbConnection.Table<Chart>()
                .Where(x => x.SongId == songId)
                .OrderBy(x => x.RatingClass)
                .ToListAsync();
        }
        public async Task<List<Chart>> GetChartsCoarse(string keyword)
        {
            keyword = keyword.ToLower();
            List<Chart> charts;
            // 尝试和 SongId 匹配
            charts = await dbConnection.Table<Chart>()
                .Where(x => x.SongId == keyword)
                .ToListAsync();
            if (charts.Count > 0)
                return charts;

            charts = await dbConnection.Table<Chart>()
                .Where(x => x.SongId.Contains(keyword))
                .ToListAsync();
            if (charts.Count > 0)
                return charts;

            // 尝试和 NameEN 与 NameJP 匹配
            charts = await dbConnection.Table<Chart>()
                .Where(x => x.NameEN.ToLower().Contains(keyword) || x.NameJP.ToLower().Contains(keyword))
                .ToListAsync();
            if (charts.Count > 0)
                return charts;

            // 尝试从 Alias 中搜索
            var aliasList = await dbConnection.Table<Alias>()
                .Where(x => x.SongAlias == keyword)
                .ToListAsync();
            if (aliasList.Count > 0)
            {
                return await GetChartsPrecise(aliasList.First().SongId);
            }

            aliasList = await dbConnection.Table<Alias>()
                .Where(x => x.SongAlias.ToLower().Contains(keyword))
                .ToListAsync();
            if (aliasList.Count > 0)
            {
                charts = new();
                foreach (var alias in aliasList)
                {
                    charts = charts.Union
                        (
                            dbConnection.Table<Chart>()
                            .Where(x => x.SongId == alias.SongId)
                            .ToListAsync().Result
                        ).ToList();
                }
                return charts;
            }

            // 尝试从aua获取
            charts = ArcaeaModule.ArcaeaUnlimitedAPI.GetSongInfo(keyword).Result?.Content?.Difficulties ?? new();
            return charts;
        }
        public async Task<List<Alias>> GetAlias(string songId)
        {
            return await dbConnection.Table<Alias>()
                .Where(x => x.SongId == songId)
                .ToListAsync();
        }
        public async Task<Package?> GetPackage(string packageId)
        {
            return await dbConnection.Table<Package>()
                .Where(x => x.PackageId == packageId)
                .FirstOrDefaultAsync();
        }
    }

    [Table("alias")]
    public class Alias
    {
        [Column("sid")]
        public string SongId { get; set; }

        [PrimaryKey]
        [Column("alias")]
        public string SongAlias { get; set; }
    }

    [Table("packages")]
    public class Package
    {
        [PrimaryKey]
        [Column("id")]
        public string PackageId { get; set; }

        [Column("name")]
        public string PackageName { get; set; } = "";
    }

    [Table("charts")]
    public class Chart
    {
        [PrimaryKey]
        [Column("song_id")]
        public string SongId { get; set; } = string.Empty;
        [PrimaryKey]
        [Column("rating_class")]
        public RatingClass RatingClass { get; set; } = RatingClass.Past;
        [Column("name_en")]
        public string NameEN { get; set; } = string.Empty;
        [Column("name_jp")]
        public string NameJP { get; set; } = string.Empty;
        [Column("artist")]
        public string Artist { get; set; } = string.Empty;
        [Column("bpm")]
        public string BPM { get; set; } = string.Empty;
        [Column("bpm_base")]
        public float BPMBase { get; set; } = 0f;
        [Column("set")]
        public string PackageId { get; set; } = string.Empty;
        [Ignore]
        public Package Package
        {
            get => ArcaeaModule.ArcaeaSongDatabase.DBConnection
                .Table<Package>()
                .FirstOrDefaultAsync(x => x.PackageId == PackageId).Result;
        }
        [Column("time")]
        public int TimeSeconds { get; set; } = 0;
        public TimeSpan Time { get => new TimeSpan(0, 0, TimeSeconds); }
        [Column("side")]
        public ChartSide ChartSide { get; set; } = ChartSide.Hikari;
        [Column("world_unlock")]
        public bool WorldUnlock { get; set; } = false;
        [Column("remote_download")]
        public bool RemoteDownload { get; set; } = false;
        [Column("bg")]
        public string Background { get; set; } = string.Empty;
        [Column("date")]
        public long DateTimeSpan { get; set; } = 0;
        [Ignore]
        public DateTime Date { get => Utils.GetUnixDateTimeSeconds(DateTimeSpan); }
        [Column("version")]
        public string Version { get; set; } = string.Empty;
        [Column("difficulty")]
        public int Difficulty { get; set; } = 0;
        [Column("rating")]
        public int Rating { get; set; } = 0;
        [Column("note")]
        public int Note { get; set; } = 0;
        [Column("chart_designer")]
        public string ChartDesigner { get; set; } = string.Empty;
        [Column("jacket_designer")]
        public string JacketDesigner { get; set; } = string.Empty;
        [Column("jacket_override")]
        public bool JacketOverride { get; set; } = false;
        [Column("audio_override")]
        public bool AudioOverride { get; set; } = false;

        [Ignore]
        public int MaxScore
            => 10_000_000 + Note;
        [Ignore]
        public string DifficultyStr
            => $"{Difficulty / 2}{(Difficulty % 2 == 0 ? "" : "+")}";
    }

    public enum RatingClass
    {
        Past = 0,
        Present,
        Future,
        Beyond,
    }

    public enum ChartSide
    {
        Hikari,     // Light
        Conflict,
        Colerless,
    }
}
