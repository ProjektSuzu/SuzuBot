using static ProjektRin.Commands.Modules.Arcaea.SongResult;

namespace ProjektRin.Commands.Modules.Arcaea
{
    internal static class SongSuggester
    {
        static ArcSongDB songDB = ArcSongDB.Instance;

        public enum TargetScore
        {
            S950W,
            S980W,
            S990W,
            S995W,
            S1000W,
        }

        public static int GetTargetScore(TargetScore score)
        {
            switch (score)
            {
                case TargetScore.S950W:
                    return 09_500_000;
                case TargetScore.S980W:
                    return 09_800_000;
                case TargetScore.S990W:
                    return 09_900_000;
                case TargetScore.S995W:
                    return 09_950_000;
                case TargetScore.S1000W:
                    return 10_000_000;
                default:
                    return 0;
            }
        }

        public class SuggestResult
        {
            public Song Song;
            public Difficulty Difficulty;
            public TargetScore TargetScore;
            public float B30Delta;
            public bool IsOverRank;
        }
        public static SuggestResult? Suggest(B30Result b30Result)
        {
            float pttIndicator = b30Result.content.account_Info.rating;
            if (pttIndicator < 0)
            {
                pttIndicator = (float)b30Result.content.best30_avg;
            }
            else
            {
                pttIndicator = (float)pttIndicator / 100 + (float)b30Result.content.best30_avg;
                pttIndicator /= 2;
            }
            Console.WriteLine(pttIndicator);
            var b30 = b30Result.content.best30_list;
            var oldB30AVG = b30Result.content.best30_avg;
            var b30Top = b30.First().rating;
            var b30Floor = b30.Last().rating;

            bool FloorLimit(Song s)
            {
                if ((float)s.RatingPST / 10 + 2 > b30Floor)
                    return true;
                else if ((float)s.RatingPRS / 10 + 2 > b30Floor)
                    return true;
                else if ((float)s.RatingFTR / 10 + 2 > b30Floor)
                    return true;
                else if (s.RatingBYD > 0 && (float)s.RatingBYD / 10 + 2 > b30Floor)
                    return true;
                else
                    return false;
            }

            bool TopLimit(Song s)
            {
                if ((float)s.RatingPST / 10 >= b30Top)
                    return false;
                else if ((float)s.RatingPRS / 10 >= b30Top)
                    return false;
                else if ((float)s.RatingFTR / 10 >= b30Top)
                    return false;
                else if (s.RatingBYD > 0 && (float)s.RatingBYD / 10 >= b30Top)
                    return false;
                else
                    return true;
            }

            var suggestSong = songDB
                .dbConnection
                .Table<Song>()
                .Where(FloorLimit)
                .Where(TopLimit)
                .ToList();



            (Difficulty, TargetScore) Calculate(Song s)
            {
                for (int i = 0; i < 4; i++)
                {
                    float rating;
                    switch (i)
                    {
                        case 0: rating = s.RatingPST; break;
                        case 1: rating = s.RatingPRS; break;
                        case 2: rating = s.RatingFTR; break;
                        case 3: rating = s.RatingBYD; break;
                        default: rating = 0; break;
                    }

                    rating = rating / 10;

                    int j = 0;
                    for (; j < 5; j++)
                    {
                        if (j == 5) break;
                        var targetScore = TargetScore.S950W + j;
                        if (CalculatePTT(rating, targetScore) > b30Floor)
                            break;
                    }

                    if (j > 3) continue;

                    return ((Difficulty)i, TargetScore.S950W + j);
                }
                return (Difficulty.Beyond, TargetScore.S1000W);
            }

            //foreach (var s in suggestSong)
            //{
            //    var (difficulty, clearType) = Calculate(s);
            //    Console.WriteLine($"{s.SongID}\t\t\t{difficulty}\t\t\t{clearType}\t\t\t{CalculatePTT(s, difficulty, clearType)}");
            //}

            while (suggestSong.Count > 0)
            {
                var song = suggestSong.ElementAt(new Random().Next(suggestSong.Count));
                suggestSong.Remove(song);

                var (difficulty, targetScore) = Calculate(song);
                List<SongResult> tempB30 = new List<SongResult>();
                //Deepcopy
                foreach (var s in b30)
                {
                    tempB30.Add(new SongResult() { song_id = s.song_id, rating = s.rating, difficulty = s.difficulty });
                }
                tempB30.Sort((a, b) => b.rating.CompareTo(a.rating));

                //Console.WriteLine(tempB30.Select(s => s.rating).Sum());

                float b30Sum;

                var songResult = tempB30.FirstOrDefault(s => s.song_id == song.SongID);
                if (songResult != null)
                {
                    var playType = songResult.GetClearType();
                    if (songResult.score >= GetTargetScore(targetScore))
                        if (playType != ClearType.PM)
                            targetScore++;
                        else
                            continue;

                    tempB30.Remove(songResult);
                    songResult.rating = CalculatePTT(song, difficulty, targetScore);
                    tempB30.Add(songResult);
                    b30Sum = (float)tempB30.Select(s => s.rating).Sum();
                }
                else
                {
                    tempB30.RemoveAt(tempB30.Count - 1);
                    b30Sum = (float)tempB30.Select(s => s.rating).Sum();
                    b30Sum += CalculatePTT(song, difficulty, targetScore);
                }

                var newB30AVG = b30Sum / 30;

                var diff = newB30AVG - oldB30AVG;
                if (diff < 0.01)
                    continue;

                var isOverRank = GetRating(song, difficulty) >= pttIndicator - 1;
                //Console.WriteLine($"{song.SongID,-32}{difficulty,-8}{clearType,-8}{CalculatePTT(song, difficulty, clearType),-8:0.00}{newB30AVG - oldB30AVG,-8:0.00}{(GetRating(song, difficulty) < ptt - 1 ? "" : "越级风险".PadLeft(4))}");
                suggestSong.Remove(song);
                return new SuggestResult()
                {
                    Song = song,
                    Difficulty = difficulty,
                    TargetScore = targetScore,
                    B30Delta = (float)(newB30AVG - oldB30AVG),
                    IsOverRank = isOverRank
                };
                //break;
            }
            return null;
        }

        public static float GetRating(Song s, Difficulty difficulty)
        {
            float rating;
            switch (difficulty)
            {
                case Difficulty.Past: rating = s.RatingPST; break;
                case Difficulty.Present: rating = s.RatingPRS; break;
                case Difficulty.Future: rating = s.RatingFTR; break;
                case Difficulty.Beyond: rating = s.RatingBYD; break;
                default: rating = 0; break;
            }
            return rating / 10;
        }

        static float CalculatePTT(Song s, Difficulty difficulty, TargetScore score)
        {
            return CalculatePTT(GetRating(s, difficulty), score);
        }

        static float CalculatePTT(float rating, TargetScore score)
        {
            //因为只用到AA及以上的 就没多写
            switch (score)
            {
                case TargetScore.S950W:
                    return rating;

                case TargetScore.S980W:
                    return rating + 1f;

                case TargetScore.S990W:
                    return rating + 1.5f;

                case TargetScore.S995W:
                    return rating + 1.75f;

                case TargetScore.S1000W:
                    return rating + 2f;

                default:
                    return -1;
            }
        }
    }
}
