namespace ProjektRin.Commands.Modules.Arcaea
{
    public class PlayResult
    {
        public int status;
        public string message;
    }
    public class UserInfoResult : PlayResult
    {
        public UserInfoContent content;
    }

    public class BestPlayResult : PlayResult
    {
        public BestContent content;
    }

    public class UserInfoContent
    {
        public AccountInfo account_info;
        public List<SongResult> recent_score;
    }

    public class BestContent
    {
        public AccountInfo account_info;
        public SongResult record;
    }

    public class SongResult
    {
        public enum Difficulty
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

        public int score;
        public int health;
        public double rating;
        public string song_id;
        public Difficulty difficulty;
        public long time_played;

        public int miss_count;
        public int near_count;
        public int perfect_count;
        public int shiny_perfect_count;

        public ClearType GetClearType()
        {
            if (score >= 10000000)
            {
                return ClearType.PM;
            }
            else if (score >= 9900000)
            {
                return ClearType.EXPlus;
            }
            else if (score >= 9800000)
            {
                return ClearType.EX;
            }
            else if (score >= 9500000)
            {
                return ClearType.AA;
            }
            else if (score >= 9200000)
            {
                return ClearType.A;
            }
            else if (score >= 8900000)
            {
                return ClearType.B;
            }
            else if (score >= 8600000)
            {
                return ClearType.C;
            }
            else
            {
                return ClearType.D;
            }
        }
    }

    public class B30Content
    {
        public double best30_avg;
        public double recent10_avg;
        public AccountInfo account_Info;
        public List<SongResult> best30_list;
        public List<SongResult> best30_overflow;
    }

    public class AccountInfo
    {
        public string code;
        public string name;
        public bool is_char_uncapped;
        public int rating;
        public int character;

        public int GetPlayerPTTType()
        {
            if (rating >= 1250)
            {
                return 6;
            }
            else if (rating >= 1200)
            {
                return 5;
            }
            else if (rating >= 1100)
            {
                return 4;
            }
            else if (rating >= 1000)
            {
                return 3;
            }
            else if (rating >= 700)
            {
                return 2;
            }
            else if (rating >= 350)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }

    public class B30Result
    {
        public int status;
        public string message;

        public B30Content content;
    }
}
