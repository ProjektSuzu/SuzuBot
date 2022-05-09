using ProjektRin.Components;
using SQLite;
using static ProjektRin.Commands.Modules.Arcaea.SongResult;

namespace ProjektRin.Commands.Modules.Arcaea
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

        public Song? TryGetSong(string name)
        {
            name = name.ToLower();
            Song? song;
            song = GetSong(name);
            if (song != null)
            {
                return song;
            }

            string? sid = dbConnection
                .Table<Alias>()
                .Where(a => a.AliasName.ToLower() == name)
                .Select(a => a.SongID)
                .ToList().FirstOrDefault();
            if (sid != null)
            {
                return GetSong(sid);
            }

            song = dbConnection
                .Table<Song>()
                .Where(a => a.NameEN.ToLower() == name || a.NameJP.ToLower() == name)
                .ToList().FirstOrDefault();
            if (song != null)
            {
                return song;
            }
            else
            {
                return null;
            }
        }

        public Song? GetSong(string sid)
        {
            return dbConnection
                .Table<Song>()
                .Where(s => s.SongID.ToLower() == sid)
                .ToList()
                .FirstOrDefault();
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

    [Table("songs")]
    internal class Song
    {
        [PrimaryKey]
        [Column("sid")]
        public string SongID { get; set; }

        [Column("name_en")]
        public string NameEN { get; set; }

        [Column("name_jp")]
        public string NameJP { get; set; }

        [Column("bpm")]
        public string BPM { get; set; }

        [Column("rating_pst")]
        public int RatingPST { get; set; }

        [Column("rating_prs")]
        public int RatingPRS { get; set; }

        [Column("rating_ftr")]
        public int RatingFTR { get; set; }

        [Column("rating_byn")]
        public int RatingBYD { get; set; }

        [Column("notes_pst")]
        public int NotePST { get; set; }

        [Column("notes_prs")]
        public int NotePRS { get; set; }

        [Column("notes_ftr")]
        public int NoteFTR { get; set; }

        [Column("notes_byn")]
        public int NoteBYD { get; set; }

        public int GetTheoreticalValue(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Past:
                    return 10000000 + NotePST;
                case Difficulty.Present:
                    return 10000000 + NotePRS;
                case Difficulty.Future:
                    return 10000000 + NoteFTR;
                case Difficulty.Beyond:
                    return 10000000 + NoteBYD;
                default:
                    return 10000000;
            }
        }
    }
}
