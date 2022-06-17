namespace RinBot.Commands.Modules.Arcaea
{
    internal static class SongSuggester
    {
        private static readonly ArcSongDB db = ArcSongDB.Instance;

        public enum SongClearRank
        {
            F,  // 低于950W都算烂了
            AA,
            EX,
            EXPlus,
            PM
        }

        private static SongClearRank GetClearRank(SongResult result)
        {
            switch (result.score)
            {
                case var s when s < 9_500_000:
                    return SongClearRank.F;
                case var s when s < 9_800_000:
                    return SongClearRank.AA;
                case var s when s < 9_950_000:
                    return SongClearRank.EX;
                case var s when s < 10_000_000:
                    return SongClearRank.EXPlus;
                case var s when s > 10_000_000:
                    return SongClearRank.PM;

                default:
                    return SongClearRank.F;
            }
        }

        public static float CalcSongList(B30Content b30)
        {
            var b30List = b30.best30_list;
            var b30Avg = b30.best30_avg;

            //最高歌曲定数不宜高于B30平均值+0.1
            var top = b30Avg + 0.1f;

            //最低歌曲定数不应低于B30地板分数
            var bottom = b30.best30_list.Last().rating;

            var candidateSongList = db.dbConnection
                .Table<Chart>()
                .Where(x => (float)x.Rating / 10 > bottom && (float)x.Rating / 10 < top);

            //过滤掉游玩过且分数达到PM的歌曲
            candidateSongList = candidateSongList.Where(x => !b30List.Any(y => y.song_id == x.SongID && y.score >= 10_000_000));

            return 114514;
        }

    }
}
