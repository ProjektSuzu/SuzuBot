using Newtonsoft.Json;

namespace RinBot.Command.Arcaea
{
    public enum Status
    {
        OK = 0,
        InvalidUserNameOrUserCode = -1,
        InvalidUserCode = -2,
        UserNotFound = -3,
        TooManyUsers = -4,
        InvalidSongNameOrSongId = -5,
        InvalidSongId = -6,
        SongNotRecorded = -7,
        TooManyRecords = -8,
        InvalidDifficulty = -9,

    }

    public class PlayerResult
    {
        [JsonProperty("status")]
        public int Status { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class PlayerInfoResult : PlayerResult
    {
        [JsonProperty("content")]
        public PlayerInfoContent Content { get; set; }
    }

    public class BestPlayResult : PlayerResult
    {
        [JsonProperty("content")]
        public BestPlayContent Content { get; set; }
    }

    public class B30Result : PlayerResult
    {
        [JsonProperty("content")]
        public B30Content Content { get; set; }
    }

    public class PlayerInfoContent
    {
        [JsonProperty("account_info")]
        public AccountInfo AccountInfo { get; set; }
        [JsonProperty("recent_score")]
        public List<SongResult> RecentScore { get; set; }
    }

    public class BestPlayContent
    {
        [JsonProperty("account_info")]
        public AccountInfo AccountInfo { get; set; }
        [JsonProperty("record")]
        public SongResult Record { get; set; }
    }

    public class B30Content
    {
        [JsonProperty("best30_avg")]
        public double B30AVG { get; set; }
        [JsonProperty("recent10_avg")]
        public double R10AVG { get; set; }
        [JsonProperty("account_info")]
        public AccountInfo AccountInfo { get; set; }
        [JsonProperty("best30_list")]
        public List<SongResult> B30List { get; set; }
        [JsonProperty("best30_overflow")]
        public List<SongResult> B30Overflow { get; set; }
    }

    public class AccountInfo
    {
        [JsonProperty("code")]
        public string UserCode { get; set; }
        [JsonProperty("name")]
        public string UserName { get; set; }
        [JsonProperty("is_char_uncapped")]
        public bool IsCharacterUncapped { get; set; }
        [JsonProperty("rating")]
        public int Rating { get; set; }
        [JsonProperty("character")]
        public int Character { get; set; }

        public int GetPlayerPTTType()
        {
            if (Rating < 0 || Rating >= 1300)
            {
                return 7;
            }
            else if (Rating >= 1250)
            {
                return 6;
            }
            else if (Rating >= 1200)
            {
                return 5;
            }
            else if (Rating >= 1100)
            {
                return 4;
            }
            else if (Rating >= 1000)
            {
                return 3;
            }
            else if (Rating >= 700)
            {
                return 2;
            }
            else if (Rating >= 350)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }

    public class SongResult
    {
        public enum SongDifficulty
        {
            Past,
            Present,
            Future,
            Beyond,
        };

        public enum ClearType
        {
            D,
            C,
            B,
            A,
            AA,
            EX,
            EXPlus,
            PM,
        }

        [JsonProperty("score")]
        public int Score { get; set; }
        [JsonProperty("health")]
        public int Health { get; set; }
        [JsonProperty("rating")]
        public double Rating { get; set; }
        [JsonProperty("song_id")]
        public string SongId { get; set; }
        [JsonProperty("difficulty")]
        public SongDifficulty Difficulty { get; set; }
        [JsonProperty("time_played")]
        private long TimePlayedTimeSpan { get; set; }
        public DateTime TimePlayed => TimeZoneInfo
                .ConvertTimeFromUtc(new DateTime(1970, 1, 1), TimeZoneInfo.Local)
                .AddMilliseconds(TimePlayedTimeSpan);

        [JsonProperty("miss_count")]
        public int LostCount { get; set; }
        [JsonProperty("near_count")]
        public int FarCount { get; set; }
        [JsonProperty("perfect_count")]
        public int PureCount { get; set; }
        [JsonProperty("shiny_perfect_count")]
        public int MaxPureCount { get; set; }

        public ClearType GetClearType()
        {
            if (Score >= 10000000)
            {
                return ClearType.PM;
            }
            else if (Score >= 9900000)
            {
                return ClearType.EXPlus;
            }
            else if (Score >= 9800000)
            {
                return ClearType.EX;
            }
            else if (Score >= 9500000)
            {
                return ClearType.AA;
            }
            else if (Score >= 9200000)
            {
                return ClearType.A;
            }
            else if (Score >= 8900000)
            {
                return ClearType.B;
            }
            else if (Score >= 8600000)
            {
                return ClearType.C;
            }
            else
            {
                return ClearType.D;
            }
        }
    }
}
