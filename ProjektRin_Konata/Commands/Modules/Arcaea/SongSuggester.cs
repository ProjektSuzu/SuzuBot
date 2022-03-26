using static ProjektRin.Commands.Modules.Arcaea.SongResult;

namespace ProjektRin.Commands.Modules.Arcaea
{
    internal static class SongSuggester
    {
        private static readonly ArcSongDB songDB = ArcSongDB.Instance;

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
        public static SuggestResult? Suggest(B30Result b30Result, float minDelta = 0.001f)
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
            //Console.WriteLine(pttIndicator);
            List<SongResult>? b30 = b30Result.content.best30_list;
            double oldB30AVG = b30Result.content.best30_avg;
            double b30Top = b30.First().rating;
            double b30Floor = b30.Last().rating;

            bool FloorLimit(Song s)
            {
                if ((float)s.RatingPST / 10 + 2 > b30Floor)
                {
                    return true;
                }
                else if ((float)s.RatingPRS / 10 + 2 > b30Floor)
                {
                    return true;
                }
                else if ((float)s.RatingFTR / 10 + 2 > b30Floor)
                {
                    return true;
                }
                else if (s.RatingBYD > 0 && (float)s.RatingBYD / 10 + 2 > b30Floor)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            bool TopLimit(Song s)
            {
                if ((float)s.RatingPST / 10 >= b30Top)
                {
                    return false;
                }
                else if ((float)s.RatingPRS / 10 >= b30Top)
                {
                    return false;
                }
                else if ((float)s.RatingFTR / 10 >= b30Top)
                {
                    return false;
                }
                else if (s.RatingBYD > 0 && (float)s.RatingBYD / 10 >= b30Top)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            List<Song>? suggestSong = songDB
                .dbConnection
                .Table<Song>()
                .Where(FloorLimit)
                .Where(TopLimit)
                .ToList();

            List<SuggestResult> results = new();


            foreach (Song? song in suggestSong)
            {
                for (int j = 0; j < 4; j++)
                {
                    Difficulty difficulty = (Difficulty)j;
                    SongResult? playResult = b30.FirstOrDefault(x => x.song_id == song.SongID && x.difficulty == difficulty);

                    float rating = 0f;
                    switch (j)
                    {
                        case 0:
                            rating = (float)song.RatingPST / 10;
                            break;

                        case 1:
                            rating = (float)song.RatingPRS / 10;
                            break;

                        case 2:
                            rating = (float)song.RatingFTR / 10;
                            break;

                        case 3:
                            rating = (float)song.RatingBYD / 10;
                            break;

                        default:
                            break;
                    }

                    if (rating < 0)
                    {
                        break;
                    }

                    for (int k = 0; k < 5; k++)
                    {
                        List<SongResult>? tempB30 = new List<SongResult>();
                        b30.ForEach(x => tempB30.Add(new SongResult() { song_id = x.song_id, difficulty = x.difficulty, score = x.score, rating = x.rating }));
                        tempB30.Sort((a, b) => a.score.CompareTo(b.score));

                        TargetScore targetScore = (TargetScore)k;
                        if (CalculatePTT(song, difficulty, targetScore) <= b30Floor)
                        {
                            continue;
                        }
                        else
                        {
                            if (playResult == null)
                            {
                                tempB30.RemoveAt(tempB30.Count - 1);
                                SongResult? newResult = new SongResult()
                                {
                                    rating = CalculatePTT(song, difficulty, targetScore)
                                };
                                tempB30.Add(newResult);
                            }
                            else
                            {
                                tempB30.RemoveAll(x => x.song_id == playResult.song_id && x.difficulty == playResult.difficulty);
                                SongResult? newResult = new SongResult()
                                {
                                    rating = CalculatePTT(song, difficulty, targetScore)
                                };
                                tempB30.Add(newResult);
                            }

                            double newB30AVG = tempB30.Sum(x => x.rating) / tempB30.Count;

                            float delta = (float)(newB30AVG - oldB30AVG);
                            if (delta < minDelta)
                            {
                                continue;
                            }

                            SuggestResult suggest = new()
                            {
                                Song = song,
                                Difficulty = difficulty,
                                TargetScore = targetScore,
                                IsOverRank = (GetRating(song, difficulty) > pttIndicator - 1),
                                B30Delta = delta
                            };

                            results.Add(suggest);
                            break;
                        }
                    }
                }
            }

            //foreach (var suggest in results)
            //{
            //    Console.WriteLine($"{suggest.Song.NameEN, -64}{suggest.Difficulty, -8}{suggest.TargetScore, -8}");
            //}

            if (results.Count > 0)
            {
                return results.ElementAt(new Random().Next(results.Count));
            }
            else
            {
                return null;
            }
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

        private static float CalculatePTT(Song s, Difficulty difficulty, TargetScore score)
        {
            return CalculatePTT(GetRating(s, difficulty), score);
        }

        private static float CalculatePTT(float rating, TargetScore score)
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
