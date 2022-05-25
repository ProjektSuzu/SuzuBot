using RinBot.Core.Components;
using SQLite;
using static RinBot.Commands.Modules.Arcaea.SongResult;

namespace RinBot.Commands.Modules.Arcaea
{
    internal class ArcSongDB
    {
        private static readonly string dbPath = Path.Combine(BotManager.resourcePath, "ArcaeaProbe_Skia/ArcaeaSongDatabase/arcsong.db");
        public SQLiteConnection dbConnection = new(dbPath);

        #region 单例模式
        private static ArcSongDB instance;
        public static ArcSongDB Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ArcSongDB();
                }
                return instance;
            }
        }
        private ArcSongDB()
        {

        }
        #endregion

        public List<Chart> TryGetSong(string name)
        {
            name = name.ToLower();
            List<Chart> songs = GetSongs(name);
            if (songs != null && songs.Count > 0)
            {
                return songs;
            }

            string? sid = dbConnection
                .Table<Alias>()
                .Where(a => a.AliasName.ToLower() == name)
                .Select(a => a.SongID)
                .ToList().FirstOrDefault();
            if (sid != null)
            {
                return GetSongs(sid);
            }

            songs = dbConnection
                .Table<Chart>()
                .Where(a => a.NameEN.ToLower() == name || a.NameJP.ToLower() == name)
                .ToList();
            if (songs != null)
            {
                return songs;
            }
            else
            {
                return new();
            }
        }

        public List<Chart> GetSongs(string sid)
        {
            return dbConnection
                .Table<Chart>()
                .Where(s => s.SongID.ToLower() == sid)
                .ToList();
        }

        public List<string> GetAlias(string sid)
        {
            return dbConnection
                .Table<Alias>()
                .Where(a => a.SongID.ToLower() == sid)
                .Select(a => a.AliasName)
                .ToList();
        }
    }

    [Table("alias")]
    internal class Alias
    {
        [Column("sid")]
        public string SongID { get; set; }
        [Column("alias")]
        public string AliasName { get; set; }
    }

    [Table("charts")]
    internal class Chart
    {
        [PrimaryKey]
        [Column("song_id")]
        public string SongID { get; set; }

        [Column("name_en")]
        public string NameEN { get; set; }

        [Column("name_jp")]
        public string NameJP { get; set; }

        [Column("bpm")]
        public string BPM { get; set; }

        [Column("rating_class")]
        public int RatingClass { get; set; }

        [Column("difficulty")]
        public int Difficulty { get; set; }

        [Column("rating")]
        public int Rating { get; set; }

        [Column("note")]
        public int Note { get; set; }

        public int GetTheoreticalValue(Difficulty difficulty)
        {
            return 10000000 + Note;
        }

        public string GetDifficultyFriendly()
        {
            int difficulty = Difficulty;
            int counter = 0;
            while (difficulty >= 2)
            {
                difficulty -= 2;
                counter++;
            }

            if (difficulty > 0)
                return counter.ToString() + "+";
            else
                return counter.ToString();
        }
    }
}
