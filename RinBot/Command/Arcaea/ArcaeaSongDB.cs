using RinBot.Core;
using SQLite;
using System.Data;

namespace RinBot.Command.Arcaea
{
    internal class ArcaeaSongDB
    {
        #region Singleton
        private static ArcaeaSongDB instance;
        public static ArcaeaSongDB Instance
        {
            get
            {
                if (instance == null) instance = new();
                return instance;
            }
        }
        private ArcaeaSongDB()
        {
            arcSongDBConnection = new(SONG_DB_PATH);
            arcAliasDBConnection = new(ALIAS_DB_PATH);
            arcAliasDBConnection.CreateTable<Alias>();
            var aliasList = arcAliasDBConnection.Table<Alias>().ToList();
            aliasList = aliasList.Union(arcSongDBConnection.Table<Alias>()).DistinctBy(x => new { x.SongId, x.AliasName }).ToList();
            arcAliasDBConnection.DeleteAll<Alias>();
            arcAliasDBConnection.InsertAll(aliasList);
        }
        #endregion
        SQLiteConnection arcSongDBConnection;
        SQLiteConnection arcAliasDBConnection;
        private static readonly string SONG_DB_PATH = Path.Combine(Global.DB_PATH, "arcsong.db");
        private static readonly string ALIAS_DB_PATH = Path.Combine(Global.DB_PATH, "arcalias.db");

        public List<Chart> TryGetSong(string name)
        {
            name = name.ToLower();
            List<Chart> songs = GetSongs(name);
            if (songs != null && songs.Count > 0)
            {
                return songs;
            }

            songs = GetSongByAlias(name);
            if (songs != null && songs.Count > 0)
            {
                return songs;
            }

            songs = arcSongDBConnection
                .Table<Chart>()
                .Where(a => a.NameEN.ToLower().Contains(name) || a.NameJP.ToLower().Contains(name))
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

        public List<Chart> GetSongByAlias(string alias)
        {
            alias = alias.ToLower();
            var list = arcAliasDBConnection.Table<Alias>()
                .Where(a => a.AliasName.ToLower() == alias)
                .Select(x => x.SongId);
            if (list.Count() <= 0)
            {
                list = arcAliasDBConnection.Table<Alias>()
                .Where(a => a.AliasName.ToLower().Contains(alias))
                .Select(x => x.SongId);
            }
            return arcSongDBConnection.Table<Chart>()
                .Where(x => list.Contains(x.SongId)).ToList();
        }

        public List<Chart> GetSongs(string songId)
        {
            return arcSongDBConnection
                .Table<Chart>()
                .Where(s => s.SongId.ToLower() == songId)
                .ToList();
        }
        public List<string> GetAlias(string sid)
        {
            return arcAliasDBConnection.Table<Alias>()
                .Where(a => a.SongId == sid)
                .Select(a => a.AliasName)
                .ToList();
        }

        public string GetPackName(string id)
        {
            return arcSongDBConnection
                .Table<Pack>()
                .Where(a => a.Id == id)
                .Select(a => a.Name)
                .FirstOrDefault(defaultValue: "");
        }
    }

    [Table("alias")]
    internal class Alias
    {
        [Column("sid")]
        public string SongId { get; set; }
        [Column("alias")]
        public string AliasName { get; set; }
    }

    [Table("charts")]
    internal class Chart
    {
        [PrimaryKey]
        [Column("song_id")]
        public string SongId { get; set; }

        [Column("name_en")]
        public string NameEN { get; set; }

        [Column("name_jp")]
        public string NameJP { get; set; }

        [Column("bpm")]
        public string BPM { get; set; }

        [Column("set")]
        public string Set { get; set; }

        [Column("side")]
        public int Side { get; set; }

        [Column("rating_class")]
        public int RatingClass { get; set; }

        [Column("difficulty")]
        public int Difficulty { get; set; }

        [Column("rating")]
        public int Rating { get; set; }

        [Column("note")]
        public int Note { get; set; }

        public int GetTheoreticalValue()
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

    [Table("packages")]
    internal class Pack
    {
        [Column("id")]
        public string Id { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }
}
