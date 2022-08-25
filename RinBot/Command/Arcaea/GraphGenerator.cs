using NLog;
using RinBot.Command.Arcaea.Database;
using SkiaSharp;
using static RinBot.Command.Arcaea.AUAResult;

namespace RinBot.Command.Arcaea
{
    internal class GraphGenerator
    {
        private static readonly string ARCAEA_RESOURCE_PATH = ArcaeaModule.RESOURCE_DIR_PATH;
        private Logger Logger = LogManager.GetLogger("ARCIMG");

        public GraphGenerator()
        {

        }

        private static readonly List<SKColor> difficultyColors = new()
        {
            new SKColor(111, 189, 209),
            new SKColor(176, 190, 126),
            new SKColor(153, 102, 153),
            new SKColor(169, 36, 61),
        };
        private static readonly List<SKColor> difficultyColorsDarker = new()
        {
            new SKColor(95, 144, 157),
            new SKColor(122, 130, 92),
            new SKColor(122, 86, 122),
            new SKColor(106, 35, 48),
        };
        private static readonly List<SKColor> progressColors = new()
        {
            SKColors.LightSkyBlue,
            SKColors.SlateBlue,
            SKColors.DarkViolet,
            SKColors.Firebrick,
        };

        public byte[] GenerateSingleSong(SongResult songResult, int id = 1)
        {
            //准备工作
            SKImageInfo imageInfo = new(1360, 600);
            SKSurface surface = SKSurface.Create(imageInfo);
            SKCanvas mainCanvas = surface.Canvas;
            //Console.WriteLine($"ID: {id}  {songResult.song_id}");

            #region 获取歌曲信息
            List<Chart> songs = ArcaeaModule.ArcaeaSongDatabase.GetChartsPrecise(songResult.SongId).Result;
            //if (songs.Count <= 0)
            //{
            //    Logger.Warn("Local DB chart not found, try API.");
            //    songs = ArcaeaUnlimitedAPI.Instance.GetSongInfo(songResult.SongId).Result.Content.Difficulties;
            //}
            var song = songs[(int)songResult.RatingClass];
            #endregion

            #region 绘制背景
            using (SKPaint bgPaint = new())
            {
                bgPaint.Color = SKColors.White;
                if (songResult.GetClearType() == SongResult.ClearType.PM)
                {
                    bgPaint.Color = SKColors.DodgerBlue;
                }

                SKRect rect = new SKRect(0, 0, 1360, 600);
                SKRoundRect roundRect = new SKRoundRect(rect, 50);
                mainCanvas.DrawRoundRect(roundRect, bgPaint);
            }
            #endregion

            #region 获取封面图片
            SKBitmap bitmap = SKBitmap.Decode(GetSongCover(ArcaeaModule.ArcaeaSongDatabase.GetChart(songResult.SongId, songResult.RatingClass).Result));
            SKBitmap scaledBitmap = new SKBitmap(580, 580);
            bitmap.ScalePixels(scaledBitmap, SKFilterQuality.None);
            #endregion

            #region 获取封面图片的平均色
            SKColor averageColor = GetAverageColor(bitmap);
            bitmap.Dispose();
            #endregion

            #region 根据平均色决定文字颜色
            SKColor textColor = SKColors.White;
            int Luminance = (int)(0.2126 * averageColor.Red + 0.7152 * averageColor.Green + 0.0722 * averageColor.Blue);
            if (Luminance > 128)
            {
                textColor = SKColors.Black;
            }
            #endregion

            #region 绘制副信息板
            using (SKPaint bgPaint = new SKPaint())
            {
                bgPaint.Color = new SKColor(64, 64, 64);
                SKRect backBoardDown = new SKRect(10, 10, 1350, 530);
                SKRoundRect backBoardDownRound = new SKRoundRect(backBoardDown, 50);
                mainCanvas.DrawRoundRect(backBoardDownRound, bgPaint);
            }
            #endregion

            #region 绘制主信息板
            using (SKPaint bgPaint = new SKPaint())
            {
                bgPaint.Color = averageColor;
                SKRect backBoardDown = new SKRect(10, 10, 1350, 340);
                SKRoundRect backBoardDownRound = new SKRoundRect(backBoardDown, 50);
                mainCanvas.DrawRoundRect(backBoardDownRound, bgPaint);
            }
            #endregion

            #region 绘制歌曲名字
            string songName = song.NameEN;
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = textColor;
                fontPaint.TextSize = 72;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.TextEncoding = SKTextEncoding.Utf8;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/NotoSans-Regular.ttf"));
                SKRect rect = new SKRect();
                float maxWidth = 700;
                fontPaint.MeasureText(songName, ref rect);
                while (rect.Size.Width > maxWidth)
                {
                    songName = songName[..^3] + "..";
                    fontPaint.MeasureText(songName, ref rect);
                }

                mainCanvas.DrawText(songName, 970, 180, fontPaint);
            }
            #endregion

            #region 绘制难度信息条
            using (SKPaint bgPaint = new SKPaint())
            {
                bgPaint.Color = difficultyColorsDarker[(int)songResult.RatingClass];
                SKRect backDifficultyBorad = new SKRect(10, 10, 1350, 110);
                SKRoundRect backDifficultyBoradRound = new SKRoundRect(backDifficultyBorad, 50);
                mainCanvas.DrawRoundRect(backDifficultyBoradRound, bgPaint);

                bgPaint.Color = difficultyColors[(int)songResult.RatingClass];
                SKRect frontDifficultyBorad = new SKRect(10, 10, 860, 110);
                SKRoundRect frontDifficultyBoradRound = new SKRoundRect(frontDifficultyBorad, 50);
                mainCanvas.DrawRoundRect(frontDifficultyBoradRound, bgPaint);
            }
            #endregion

            #region 绘制歌曲PTT
            string songPTT = $"{songResult.Rating:0.0000}";
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-Regular.ttf"));
                SKRect rect = new SKRect();
                fontPaint.MeasureText(songPTT, ref rect);
                float w = rect.Size.Width;
                float h = rect.Size.Height;

                mainCanvas.DrawText(songPTT, 710, 80, fontPaint);
            }
            #endregion

            #region 绘制歌曲定数
            string songRating = "WTF?!";
            switch (songResult.RatingClass)
            {
                case RatingClass.Beyond:
                    songRating = $"BEYOND {(float)song.Rating / 10:0.00}";
                    break;

                case RatingClass.Future:
                    songRating = $"FUTURE {(float)song.Rating / 10:0.00}";
                    break;

                case RatingClass.Present:
                    songRating = $"PRESENT {(float)song.Rating / 10:0.00}";
                    break;

                case RatingClass.Past:
                    songRating = $"PAST {(float)song.Rating / 10:0.00}";
                    break;

                default:
                    break;
            }
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-Regular.ttf"));
                SKRect rect = new SKRect();
                fontPaint.MeasureText(songRating, ref rect);
                float w = rect.Size.Width;
                float h = rect.Size.Height;

                mainCanvas.DrawText(songRating, 1080, 80, fontPaint);
            }
            #endregion

            #region 绘制歌曲分数
            string songScore = FormatScore(songResult.Score);
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = textColor;
                fontPaint.TextSize = 128;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-Regular.ttf"));

                mainCanvas.DrawText(songScore, 970, 300, fontPaint);
            }
            #endregion

            #region 绘制PFL条
            int total_count = songResult.LostCount + songResult.FarCount + songResult.PureCount;
            using (SKPaint bgPaint = new SKPaint())
            {
                //Lost
                bgPaint.Color = SKColors.LimeGreen;
                SKRect lostRect = new SKRect(620, 420, 1320, 440);
                SKRoundRect lostRectRound = new SKRoundRect(lostRect, 10);
                mainCanvas.DrawRoundRect(lostRectRound, bgPaint);

                //Far
                bgPaint.Color = SKColors.Yellow;
                SKRect farRect = new SKRect(620, 420, 620 + (float)(songResult.FarCount + songResult.PureCount) / total_count * 700, 440);
                SKRoundRect farRectRound = new SKRoundRect(farRect, 10);
                mainCanvas.DrawRoundRect(farRectRound, bgPaint);

                //Pure
                bgPaint.Color = SKColors.DodgerBlue;
                SKRect pureRect = new SKRect(620, 420, 620 + (float)songResult.PureCount / total_count * 700, 440);
                SKRoundRect pureRectRound = new SKRoundRect(pureRect, 10);
                mainCanvas.DrawRoundRect(pureRectRound, bgPaint);

                //MaxPure
                bgPaint.Color = SKColors.Violet;
                SKRect maxPureRect = new SKRect(620, 420, 620 + (float)songResult.MaxPureCount / total_count * 700, 440);
                SKRoundRect maxPureRectRound = new SKRoundRect(maxPureRect, 10);
                mainCanvas.DrawRoundRect(maxPureRectRound, bgPaint);
            }
            #endregion

            #region 绘制PFL文字
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.DodgerBlue;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Right;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-Regular.ttf"));

                //Pure
                string pureText = $"P:{songResult.PureCount}";
                float pureTextOffset = (float)songResult.PureCount / total_count * 700;
                if (pureTextOffset < 200)
                {
                    pureTextOffset = 200;
                }

                mainCanvas.DrawText(pureText, 620 + pureTextOffset / 2, 400, fontPaint);

                fontPaint.Color = SKColors.Violet;
                fontPaint.TextAlign = SKTextAlign.Left;
                string maxPureText = $"[{songResult.MaxPureCount}]";
                mainCanvas.DrawText(maxPureText, 620 + pureTextOffset / 2, 400, fontPaint);

                //Far
                fontPaint.Color = SKColors.Yellow;
                fontPaint.TextAlign = SKTextAlign.Right;
                string farText = $"F:{songResult.FarCount}";
                float farTextOffset = (float)songResult.FarCount / total_count * 700 + pureTextOffset;
                if (farTextOffset < 200)
                {
                    farTextOffset = 200;
                }

                if (farTextOffset > 700)
                {
                    farTextOffset = 700;
                }

                mainCanvas.DrawText(farText, 620 + farTextOffset, 500, fontPaint);

                //Lost
                fontPaint.Color = SKColors.LimeGreen;
                fontPaint.TextAlign = SKTextAlign.Right;
                string lostText = $"L:{songResult.LostCount}";
                float lostTextOffset = (float)songResult.LostCount / total_count * 700;

                mainCanvas.DrawText(lostText, 1320 - lostTextOffset / 2, 400, fontPaint);
            }
            #endregion

            #region 绘制封面
            SKImage cover = SKImage.FromBitmap(scaledBitmap);
            SKSurface coverSurface = SKSurface.Create(new SKImageInfo(580, 580));
            SKCanvas coverCanvas = coverSurface.Canvas;

            SKRect clipRect = new SKRect(0, 0, 580, 580);
            SKRoundRect clipRoundRect = new SKRoundRect(clipRect, 50);
            coverCanvas.ClipRoundRect(clipRoundRect, SKClipOperation.Intersect, true);
            coverCanvas.DrawImage(cover, 0, 0);

            coverSurface.Draw(mainCanvas, 10, 10, null);
            scaledBitmap.Dispose();
            coverSurface.Dispose();
            coverCanvas.Dispose();
            cover.Dispose();
            #endregion

            #region 绘制游玩时间和序号
            DateTime playTime = songResult.TimePlayed;
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.Black;
                fontPaint.TextSize = 60;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/ark-pixel-12px-latin.otf"));

                mainCanvas.DrawText(playTime.ToString("yyyy-MM-dd HH:mm:ss"), 600, 585, fontPaint);

                fontPaint.TextAlign = SKTextAlign.Right;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-Regular.ttf"));

                string idStr = $"#{id.ToString().PadLeft(2, '0')}";
                mainCanvas.DrawText(idStr, 1320, 585, fontPaint);
            }
            #endregion

            SKImage image = surface.Snapshot();
            byte[] data = image.Encode(SKEncodedImageFormat.Png, 80).ToArray();
            surface.Dispose();
            mainCanvas.Dispose();
            image.Dispose();
            return data;
        }

        public byte[] GenerateBest30(B30Result b30Result)
        {
            //准备工作
            SKImageInfo imageInfo = new SKImageInfo(2400, 5000);
            SKSurface surface = SKSurface.Create(imageInfo);
            SKCanvas mainCanvas = surface.Canvas;

            #region 绘制背景
            using (SKPaint bgPaint = new SKPaint())
            {
                SKBitmap background = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(ARCAEA_RESOURCE_PATH, "background.jpg")));
                bgPaint.ImageFilter = SKImageFilter.CreateBlur(10, 10);
                bgPaint.IsAntialias = true;
                SKBitmap scaledBitmap = new SKBitmap(imageInfo);
                background.ScalePixels(scaledBitmap, SKFilterQuality.Low);
                mainCanvas.DrawBitmap(scaledBitmap, 0, 0, bgPaint);

                bgPaint.ImageFilter = null;
                bgPaint.Color = new SKColor(0, 0, 0, 64);
                SKRect rect = new SKRect(0, 0, 2400, 5000);
                mainCanvas.DrawRect(rect, bgPaint);
            }
            #endregion

            #region 获取用户信息
            AccountInfo playerInfo = b30Result.Content.AccountInfo;
            #endregion

            #region 绘制玩家信息框
            using (SKPaint bgPaint = new SKPaint())
            {
                SKBitmap bmp = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(ARCAEA_RESOURCE_PATH, "user_back.png")));
                SKBitmap scaledBitmap = new SKBitmap(900, 250);
                bmp.ScalePixels(scaledBitmap, SKFilterQuality.None);

                mainCanvas.DrawBitmap(scaledBitmap, 345, 160);
            }
            #endregion

            #region 绘制搭档图标
            using (SKPaint bgPaint = new SKPaint())
            {
                SKBitmap bmp = GetCharaIcon((uint)playerInfo.Character, playerInfo.IsCharacterUncapped);
                SKBitmap scaledBitmap = new SKBitmap(400, 400);
                bmp.ScalePixels(scaledBitmap, SKFilterQuality.Medium);

                mainCanvas.DrawBitmap(scaledBitmap, 50, 50);
            }
            #endregion

            #region 绘制PTT边框
            using (SKPaint bgPaint = new SKPaint())
            {
                SKBitmap bmp = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(ARCAEA_RESOURCE_PATH, $"rating/rating_{playerInfo.GetPlayerPTTType()}.png")));
                SKBitmap scaledBitmap = new SKBitmap(260, 260);
                bmp.ScalePixels(scaledBitmap, SKFilterQuality.Medium);

                mainCanvas.DrawBitmap(scaledBitmap, 280, 240);
            }
            #endregion

            #region 绘制PTT
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.TextSize = 128;
                fontPaint.IsAntialias = true;


                if (playerInfo.Rating < 0)
                {
                    string pttStr = "您";
                    fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/NotoSansCJKsc-Regular.otf"));
                    fontPaint.TextAlign = SKTextAlign.Center;
                    fontPaint.Color = new SKColor(80, 73, 89);
                    fontPaint.Style = SKPaintStyle.Stroke;
                    fontPaint.StrokeWidth = 16;
                    mainCanvas.DrawText(pttStr, 410, 410, fontPaint);
                    fontPaint.Color = SKColors.White;
                    fontPaint.Style = SKPaintStyle.Fill;
                    mainCanvas.DrawText(pttStr, 410, 410, fontPaint);
                }
                else
                {
                    string pttInt = (playerInfo.Rating / 100).ToString() + ".";
                    string pttDec = (playerInfo.Rating % 100).ToString().PadLeft(2, '0');

                    fontPaint.TextAlign = SKTextAlign.Right;
                    fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-SemiBold.ttf"));
                    fontPaint.Color = new SKColor(80, 73, 89);
                    fontPaint.Style = SKPaintStyle.Stroke;
                    fontPaint.StrokeWidth = 16;
                    mainCanvas.DrawText(pttInt, 430, 420, fontPaint);
                    fontPaint.Color = SKColors.White;
                    fontPaint.Style = SKPaintStyle.Fill;
                    mainCanvas.DrawText(pttInt, 430, 420, fontPaint);

                    fontPaint.Color = new SKColor(80, 73, 89);
                    fontPaint.TextSize = 84;
                    fontPaint.TextAlign = SKTextAlign.Left;
                    fontPaint.Style = SKPaintStyle.Stroke;
                    fontPaint.StrokeWidth = 16;
                    mainCanvas.DrawText(pttDec, 430, 420, fontPaint);
                    fontPaint.Color = SKColors.White;
                    fontPaint.Style = SKPaintStyle.Fill;
                    mainCanvas.DrawText(pttDec, 430, 420, fontPaint);
                }
            }
            #endregion

            #region 绘制玩家姓名
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 144;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/GeosansLight.ttf"));

                mainCanvas.DrawText(playerInfo.UserName, 460, 300, fontPaint);
            }
            #endregion

            #region 绘制玩家代码
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/GeosansLight.ttf"));

                mainCanvas.DrawText(playerInfo.UserCode.ToString().Insert(6, " ").Insert(3, " "), 550, 400, fontPaint);
            }
            #endregion

            #region 绘制分割线
            using (SKPaint bgPaint = new SKPaint())
            {
                bgPaint.Color = SKColors.White;
                SKRect rect = new SKRect(50, 570, 2350, 580);

                mainCanvas.DrawRect(rect, bgPaint);
            }
            #endregion

            #region 绘制Bot信息
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/ark-pixel-12px-latin.otf"));

                mainCanvas.DrawText("Generated By RinBot", 50, 550, fontPaint);
            }
            #endregion

            #region 绘制生成时间
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Right;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/ark-pixel-12px-latin.otf"));

                mainCanvas.DrawText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2350, 550, fontPaint);
            }
            #endregion

            #region 绘制B30分割板和标题
            using (SKPaint fontPaint = new SKPaint())
            {
                SKBitmap bitmap = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(ARCAEA_RESOURCE_PATH, "spliter.png")));
                SKBitmap scaledBitmap = new SKBitmap(630, 126);
                bitmap.ScalePixels(scaledBitmap, SKFilterQuality.Medium);

                mainCanvas.DrawBitmap(scaledBitmap, 1200 - scaledBitmap.Width / 2, 525);
                bitmap.Dispose();
                scaledBitmap.Dispose();

                fontPaint.TextSize = 96;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-Regular.ttf"));

                fontPaint.Color = new SKColor(80, 73, 89);
                fontPaint.Style = SKPaintStyle.Stroke;
                fontPaint.StrokeWidth = 16;
                mainCanvas.DrawText("BEST30", 1200, 610, fontPaint);
                fontPaint.Color = SKColors.White;
                fontPaint.Style = SKPaintStyle.Fill;
                mainCanvas.DrawText("BEST30", 1200, 610, fontPaint);
            }
            #endregion

            #region 生成并绘制歌曲成绩图
            List<Task> tasks = new List<Task>();
            void GenerateCallback(SKBitmap bitmap, int id)
            {
                int row = id / 3;
                int col = id % 3;

                mainCanvas.DrawBitmap(bitmap, 1200 - 340 + (col - 1) * 770, row * 380 + 700);
                bitmap.Dispose();
            }
            for (int i = 0; i < b30Result.Content.B30List.Count; i++)
            {
                int id = i;
                Task? task = new Task(() =>
                {
                    SKBitmap singleSongBmp = SKBitmap.Decode(GenerateSingleSong(b30Result.Content.B30List[id], id + 1));
                    SKBitmap scaledBitmap = new SKBitmap(680, 300);
                    singleSongBmp.ScalePixels(scaledBitmap, SKFilterQuality.Medium);
                    singleSongBmp.Dispose();
                    GenerateCallback(scaledBitmap, id);
                    scaledBitmap.Dispose();
                });
                tasks.Add(task);
                task.Start();
            }
            Task.WaitAll(tasks.ToArray());
            tasks.Clear();
            #endregion

            #region 绘制分割线
            using (SKPaint bgPaint = new SKPaint())
            {
                bgPaint.Color = SKColors.White;
                SKRect rect = new SKRect(50, 4520, 2350, 4530);

                mainCanvas.DrawRect(rect, bgPaint);
            }
            #endregion

            #region 绘制Overflow分割板和标题
            using (SKPaint fontPaint = new SKPaint())
            {
                SKBitmap bitmap = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(ARCAEA_RESOURCE_PATH, "spliter.png")));
                SKBitmap scaledBitmap = new SKBitmap(630, 126);
                bitmap.ScalePixels(scaledBitmap, SKFilterQuality.Medium);

                mainCanvas.DrawBitmap(scaledBitmap, 1200 - scaledBitmap.Width / 2, 4475);
                bitmap.Dispose();
                scaledBitmap.Dispose();

                fontPaint.TextSize = 96;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-Regular.ttf"));

                fontPaint.Color = new SKColor(80, 73, 89);
                fontPaint.Style = SKPaintStyle.Stroke;
                fontPaint.StrokeWidth = 16;
                mainCanvas.DrawText("OVERFLOW", 1200, 4560, fontPaint);
                fontPaint.Color = SKColors.White;
                fontPaint.Style = SKPaintStyle.Fill;
                mainCanvas.DrawText("OVERFLOW", 1200, 4560, fontPaint);
            }
            #endregion                        

            #region 生成并绘制歌曲成绩图
            if (b30Result.Content.B30Overflow != null && b30Result.Content.B30Overflow.Count > 0)
            {
                for (int i = 0; i < b30Result.Content.B30Overflow.Count; i++)
                {
                    SKBitmap singleSongBmp = SKBitmap.Decode(GenerateSingleSong(b30Result.Content.B30Overflow[i], i + 31));
                    SKBitmap scaledBitmap = new SKBitmap(680, 300);
                    singleSongBmp.ScalePixels(scaledBitmap, SKFilterQuality.Medium);

                    int row = i / 3;
                    int col = i % 3;

                    mainCanvas.DrawBitmap(scaledBitmap, 1200 - 340 + (col - 1) * 770, 4640);
                    scaledBitmap.Dispose();
                    singleSongBmp.Dispose();
                }
            }
            else
            {
                using (SKPaint fontPaint = new SKPaint())
                {
                    fontPaint.TextSize = 128;
                    fontPaint.IsAntialias = true;
                    fontPaint.TextAlign = SKTextAlign.Center;
                    fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-Regular.ttf"));

                    fontPaint.Color = new SKColor(80, 73, 89);
                    fontPaint.Style = SKPaintStyle.Stroke;
                    fontPaint.StrokeWidth = 16;
                    mainCanvas.DrawText("No Overflow", 1200, 4800, fontPaint);

                    fontPaint.Color = SKColors.White;
                    fontPaint.Style = SKPaintStyle.Fill;
                    mainCanvas.DrawText("No Overflow", 1200, 4800, fontPaint);
                }
            }
            #endregion

            #region 绘制信息框和PTT信息
            string b30AVG = b30Result.Content.B30AVG.ToString("0.0000");
            string r10AVG = b30Result.Content.R10AVG.ToString("0.0000");
            string maxEST = ((b30Result.Content.B30AVG + (b30Result.Content.B30List.Take(10).Sum(x => x.Rating) / 10)) / 2).ToString("0.0000");
            using (SKPaint fontPaint = new SKPaint())
            {
                SKBitmap infoBorad = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(ARCAEA_RESOURCE_PATH, "res_scoresection_beyond.png")));
                SKBitmap scaledBitmap = new SKBitmap(1000, 500);
                infoBorad.ScalePixels(scaledBitmap, SKFilterQuality.None);
                mainCanvas.DrawBitmap(scaledBitmap, 2400 - scaledBitmap.Width, 0);

                infoBorad.Dispose();
                scaledBitmap.Dispose();

                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 84;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Right;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-Regular.ttf"));

                fontPaint.Color = new SKColor(80, 73, 89);
                fontPaint.Style = SKPaintStyle.Stroke;
                fontPaint.StrokeWidth = 16;
                mainCanvas.DrawText($"B30 AVG: {b30AVG}", 2300, 130, fontPaint);
                fontPaint.Color = SKColors.White;
                fontPaint.Style = SKPaintStyle.Fill;
                mainCanvas.DrawText($"B30 AVG: {b30AVG}", 2300, 130, fontPaint);

                fontPaint.Color = new SKColor(80, 73, 89);
                fontPaint.Style = SKPaintStyle.Stroke;
                fontPaint.StrokeWidth = 16;
                mainCanvas.DrawText($"R10 AVG: {r10AVG}", 2300, 240, fontPaint);
                fontPaint.Color = SKColors.White;
                fontPaint.Style = SKPaintStyle.Fill;
                mainCanvas.DrawText($"R10 AVG: {r10AVG}", 2300, 240, fontPaint);

                fontPaint.Color = new SKColor(80, 73, 89);
                fontPaint.Style = SKPaintStyle.Stroke;
                fontPaint.StrokeWidth = 16;
                mainCanvas.DrawText($"MAX EST: {maxEST}", 2300, 350, fontPaint);
                fontPaint.Color = SKColors.White;
                fontPaint.Style = SKPaintStyle.Fill;
                mainCanvas.DrawText($"MAX EST: {maxEST}", 2300, 350, fontPaint);
            }
            #endregion

            SKBitmap image = SKBitmap.FromImage(surface.Snapshot());
            SKBitmap scaledImage = new SKBitmap((int)(image.Width * 0.6), (int)(image.Height * 0.6));
            image.ScalePixels(scaledImage, SKFilterQuality.Medium);
            byte[] data = scaledImage.Encode(SKEncodedImageFormat.Jpeg, 70).ToArray();
            surface.Dispose();
            mainCanvas.Dispose();
            scaledImage.Dispose();
            image.Dispose();
            return data;
        }

        public byte[] GeneratePlayerResult(PlayerResult playerResult)
        {
            //准备工作
            SKImageInfo imageInfo = new SKImageInfo(1800, 1000);
            SKSurface surface = SKSurface.Create(imageInfo);
            SKCanvas mainCanvas = surface.Canvas;

            #region 获取游玩数据
            AccountInfo accountInfo;
            SongResult songResult;
            Chart songInfo;
            if (playerResult is PlayerInfoResult)
            {
                accountInfo = ((PlayerInfoResult)playerResult).Content.AccountInfo;
                songResult = ((PlayerInfoResult)playerResult).Content.RecentScore.First();
            }
            else
            {
                accountInfo = ((BestPlayResult)playerResult).Content.AccountInfo;
                songResult = ((BestPlayResult)playerResult).Content.Record;
            }
            var songs = ArcaeaModule.ArcaeaSongDatabase.GetChartsPrecise(songResult.SongId).Result;
            //if (songs.Count <= 0)
            //{
            //    Logger.Warn("Local DB chart not found, try API.");
            //    songs = ArcaeaUnlimitedAPI.Instance.GetSongInfo(songResult.SongId).Result.Content.Difficulties;
            //}
            songInfo = songs[(int)songResult.RatingClass];
            #endregion

            #region 绘制背景
            using (SKPaint bgPaint = new())
            {
                SKBitmap background = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(ARCAEA_RESOURCE_PATH, "beyond.png")));
                bgPaint.ImageFilter = SKImageFilter.CreateBlur(10, 10);
                bgPaint.IsAntialias = true;
                mainCanvas.DrawBitmap(background, 0, 0, bgPaint);

                background.Dispose();

                bgPaint.ImageFilter = null;
                bgPaint.Color = new SKColor(0, 0, 0, 64);
                SKRect rect = new SKRect(0, 0, 1800, 1000);
                mainCanvas.DrawRect(rect, bgPaint);
            }
            #endregion

            #region 获取封面图片
            SKBitmap bitmap = SKBitmap.Decode(GetSongCover(ArcaeaModule.ArcaeaSongDatabase.GetChart(songResult.SongId, songResult.RatingClass).Result));
            SKBitmap scaledBitmap = new SKBitmap(550, 550);
            bitmap.ScalePixels(scaledBitmap, SKFilterQuality.None);
            #endregion

            #region 获取封面图片的平均色
            SKColor averageColor = GetAverageColor(bitmap);
            bitmap.Dispose();
            #endregion

            #region 根据平均色决定文字颜色
            SKColor textColor = SKColors.White;
            int Luminance = (int)(0.2126 * averageColor.Red + 0.7152 * averageColor.Green + 0.0722 * averageColor.Blue);
            if (Luminance > 128)
            {
                textColor = SKColors.Black;
            }
            #endregion

            #region 绘制背景板
            using (SKPaint bgPaint = new SKPaint())
            {
                bgPaint.Color = new SKColor(128, 128, 128, 172);
                SKRect backBoardDown = new SKRect(50, 50, 1750, 950);
                SKRoundRect backBoardDownRound = new SKRoundRect(backBoardDown, 50);
                mainCanvas.DrawRoundRect(backBoardDownRound, bgPaint);
            }
            #endregion

            #region 绘制歌曲名背景板和曲名
            string songName = songInfo.NameEN;
            using (SKPaint fontPaint = new SKPaint())
            using (SKPaint bgPaint = new SKPaint())
            {
                bgPaint.Color = difficultyColors[(int)songResult.RatingClass];
                SKRect backBoardDown = new SKRect(70, 220, 1730, 340);
                SKRoundRect backBoardDownRound = new SKRoundRect(backBoardDown, 50);
                mainCanvas.DrawRoundRect(backBoardDownRound, bgPaint);

                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 96;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.TextEncoding = SKTextEncoding.Utf8;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/NotoSans-Regular.ttf"));
                SKRect rect = new SKRect();
                float maxWidth = 1600;
                fontPaint.MeasureText(songName, ref rect);
                while (rect.Size.Width > maxWidth)
                {
                    songName = songName[..^3] + "..";
                    fontPaint.MeasureText(songName, ref rect);
                }
                mainCanvas.DrawText(songName, 900, 310, fontPaint);
            }
            #endregion

            #region 绘制搭档立绘
            using (SKBitmap chara = GetCharaImg((uint)accountInfo.Character, accountInfo.IsCharacterUncapped))
            {
                SKBitmap scaledCharaBitmap = new SKBitmap(1500, 1500);
                chara.ScalePixels(scaledCharaBitmap, SKFilterQuality.Low);

                mainCanvas.DrawBitmap(scaledCharaBitmap, 800, 0);
                scaledCharaBitmap.Dispose();
            }
            #endregion

            #region 绘制副信息板
            using (SKPaint bgPaint = new SKPaint())
            {
                bgPaint.Color = new SKColor(64, 64, 64);
                SKRect backBoardDown = new SKRect(70, 380, 1320, 930);
                SKRoundRect backBoardDownRound = new SKRoundRect(backBoardDown, 50);
                mainCanvas.DrawRoundRect(backBoardDownRound, bgPaint);
            }
            #endregion

            #region 绘制主信息板
            using (SKPaint bgPaint = new SKPaint())
            {
                bgPaint.Color = averageColor;
                SKRect backBoardDown = new SKRect(70, 380, 1320, 710);
                SKRoundRect backBoardDownRound = new SKRoundRect(backBoardDown, 50);
                mainCanvas.DrawRoundRect(backBoardDownRound, bgPaint);
            }
            #endregion

            #region 绘制封面
            SKImage cover = SKImage.FromBitmap(scaledBitmap);
            SKSurface coverSurface = SKSurface.Create(new SKImageInfo(550, 550));
            SKCanvas coverCanvas = coverSurface.Canvas;

            SKRect clipRect = new SKRect(0, 0, 550, 550);
            SKRoundRect clipRoundRect = new SKRoundRect(clipRect, 50);
            coverCanvas.ClipRoundRect(clipRoundRect, SKClipOperation.Intersect, true);
            coverCanvas.DrawImage(cover, 0, 0);

            coverSurface.Draw(mainCanvas, 70, 380, null);
            cover.Dispose();
            coverSurface.Dispose();
            coverCanvas.Dispose();
            scaledBitmap.Dispose();
            #endregion

            #region 绘制歌曲定数和PTT
            string songRating = "WTF?!";
            switch (songResult.RatingClass)
            {
                case RatingClass.Beyond:
                    songRating = $"{(float)songInfo.Rating / 10:0.00}";
                    break;

                case RatingClass.Future:
                    songRating = $"{(float)songInfo.Rating / 10:0.00}";
                    break;

                case RatingClass.Present:
                    songRating = $"{(float)songInfo.Rating / 10:0.00}";
                    break;

                case RatingClass.Past:
                    songRating = $"{(float)songInfo.Rating / 10:0.00}";
                    break;

                default:
                    break;
            }
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = textColor;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.TextEncoding = SKTextEncoding.Utf8;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-Regular.ttf"));

                mainCanvas.DrawText($"PTT {songRating} => {songResult.Rating.ToString("0.0000")}", 965, 640, fontPaint);
            }

            #endregion
            #region 绘制歌曲得分进度
            int theoretical = songInfo.MaxScore;
            float percentage;
            int diff;
            string progressStr;
            int progress;
            switch (songResult.GetClearType())
            {
                case SongResult.ClearType.PM:
                    progress = 3;
                    diff = theoretical - songResult.Score;
                    if (diff != 0)
                    {
                        percentage = (float)diff / (theoretical - 10_000_000);
                        progressStr = $"MAX  -{FormatScore(diff)}";
                    }
                    else
                    {
                        percentage = 1;
                        progressStr = "MAX";
                    }
                    break;

                case SongResult.ClearType.EXPlus:
                    progress = 3;
                    diff = 10_000_000 - songResult.Score;
                    percentage = (float)diff / 00_100_000;
                    progressStr = $"PM  -{FormatScore(diff)}";
                    break;

                case SongResult.ClearType.EX:
                    progress = 3;
                    diff = 09_900_000 - songResult.Score;
                    percentage = (float)diff / 00_100_000;
                    progressStr = $"EX+  -{FormatScore(diff)}";
                    break;

                case SongResult.ClearType.AA:
                    progress = 2;
                    diff = 09_800_000 - songResult.Score;
                    percentage = (float)diff / 00_300_000;
                    progressStr = $"EX  -{FormatScore(diff)}";
                    break;

                case SongResult.ClearType.A:
                    progress = 2;
                    diff = 09_500_000 - songResult.Score;
                    percentage = (float)diff / 00_300_000;
                    progressStr = $"AA  -{FormatScore(diff)}";
                    break;

                case SongResult.ClearType.B:
                    progress = 1;
                    diff = 09_200_000 - songResult.Score;
                    percentage = (float)diff / 00_300_000;
                    progressStr = $"A  -{FormatScore(diff)}";
                    break;

                case SongResult.ClearType.C:
                    progress = 1;
                    diff = 08_900_000 - songResult.Score;
                    percentage = (float)diff / 00_300_000;
                    progressStr = $"B  -{FormatScore(diff)}";
                    break;

                default:
                    progress = 1;
                    percentage = 0;
                    progressStr = "D";
                    break;
            }

            using (SKPaint fontPaint = new SKPaint())
            using (SKPaint bgPaint = new SKPaint())
            {
                bgPaint.Color = progressColors[progress - 1];
                SKRect progressBarBack = new SKRect(670, 500, 1270, 550);
                SKRoundRect progressBarRoundBack = new SKRoundRect(progressBarBack, 25);
                mainCanvas.DrawRoundRect(progressBarRoundBack, bgPaint);

                bgPaint.Color = progressColors[progress];
                SKRect progressBarFront = new SKRect(670, 500, 670 + 600 * (1f - percentage), 550);
                SKRoundRect progressBarRoundFront = new SKRoundRect(progressBarFront, 25);
                mainCanvas.DrawRoundRect(progressBarRoundFront, bgPaint);

                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 56;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.TextEncoding = SKTextEncoding.Utf8;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-Regular.ttf"));

                mainCanvas.DrawText(progressStr, 970, 545, fontPaint);
            }
            #endregion

            #region 绘制歌曲分数
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = textColor;
                fontPaint.TextSize = 96;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.TextEncoding = SKTextEncoding.Utf8;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-Regular.ttf"));

                mainCanvas.DrawText(FormatScore(songResult.Score), 965, 480, fontPaint);
            }
            #endregion

            #region 绘制PFL条
            int total_count = songResult.LostCount + songResult.FarCount + songResult.PureCount;
            using (SKPaint bgPaint = new SKPaint())
            {
                //Lost
                bgPaint.Color = SKColors.LimeGreen;
                SKRect lostRect = new SKRect(670, 800, 1270, 850);
                SKRoundRect lostRectRound = new SKRoundRect(lostRect, 25);
                mainCanvas.DrawRoundRect(lostRectRound, bgPaint);

                //Far
                bgPaint.Color = SKColors.Yellow;
                SKRect farRect = new SKRect(670, 800, 670 + (float)(songResult.FarCount + songResult.PureCount) / total_count * 600, 850);
                SKRoundRect farRectRound = new SKRoundRect(farRect, 25);
                mainCanvas.DrawRoundRect(farRectRound, bgPaint);

                //Pure
                bgPaint.Color = SKColors.DodgerBlue;
                SKRect pureRect = new SKRect(670, 800, 670 + (float)songResult.PureCount / total_count * 600, 850);
                SKRoundRect pureRectRound = new SKRoundRect(pureRect, 25);
                mainCanvas.DrawRoundRect(pureRectRound, bgPaint);

                //MaxPure
                bgPaint.Color = SKColors.Violet;
                SKRect maxPureRect = new SKRect(670, 800, 670 + (float)songResult.MaxPureCount / total_count * 600, 850);
                SKRoundRect maxPureRectRound = new SKRoundRect(maxPureRect, 25);
                mainCanvas.DrawRoundRect(maxPureRectRound, bgPaint);
            }
            #endregion

            #region 绘制PFL文字
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.DodgerBlue;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Right;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-Regular.ttf"));

                //Pure
                string pureText = $"P:{songResult.PureCount}";
                float pureTextOffset = (float)songResult.PureCount / total_count * 600;
                if (pureTextOffset < 200)
                {
                    pureTextOffset = 200;
                }

                mainCanvas.DrawText(pureText, 670 + pureTextOffset / 2, 780, fontPaint);

                fontPaint.Color = SKColors.Violet;
                fontPaint.TextAlign = SKTextAlign.Left;
                string maxPureText = $"[{songResult.MaxPureCount}]";
                mainCanvas.DrawText(maxPureText, 670 + pureTextOffset / 2, 780, fontPaint);

                //Far
                fontPaint.Color = SKColors.Yellow;
                fontPaint.TextAlign = SKTextAlign.Right;
                string farText = $"F:{songResult.FarCount}";
                float farTextOffset = (float)songResult.FarCount / total_count * 600 + pureTextOffset;
                if (farTextOffset < 200)
                {
                    farTextOffset = 200;
                }

                if (farTextOffset > 700)
                {
                    farTextOffset = 700;
                }

                mainCanvas.DrawText(farText, 670 + farTextOffset, 910, fontPaint);

                //Lost
                fontPaint.Color = SKColors.LimeGreen;
                fontPaint.TextAlign = SKTextAlign.Right;
                string lostText = $"L:{songResult.LostCount}";
                float lostTextOffset = (float)songResult.LostCount / total_count * 600;

                mainCanvas.DrawText(lostText, 1320 - lostTextOffset / 2, 780, fontPaint);
            }
            #endregion

            #region 绘制玩家信息框
            using (SKBitmap userBack = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(ARCAEA_RESOURCE_PATH, "user_back.png"))))
            {
                SKBitmap scaledUserBack = new SKBitmap(400, 115);
                userBack.ScalePixels(scaledUserBack, SKFilterQuality.Low);

                mainCanvas.DrawBitmap(scaledUserBack, 125, 80);
                scaledUserBack.Dispose();
            }
            #endregion

            #region 绘制PTT边框
            using (SKPaint bgPaint = new SKPaint())
            {
                SKBitmap pttBmp = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(ARCAEA_RESOURCE_PATH, $"rating/rating_{accountInfo.GetPlayerPTTType()}.png")));
                SKBitmap scaledPTTBmp = new SKBitmap(160, 160);
                pttBmp.ScalePixels(scaledPTTBmp, SKFilterQuality.Low);

                mainCanvas.DrawBitmap(scaledPTTBmp, 50, 60);
                scaledPTTBmp.Dispose();
                pttBmp.Dispose();
            }
            #endregion

            #region 绘制PTT
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.TextSize = 84;
                fontPaint.IsAntialias = true;

                if (accountInfo.Rating < 0)
                {
                    string pttStr = "您";
                    fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/NotoSansCJKsc-Regular.otf"));
                    fontPaint.TextAlign = SKTextAlign.Center;
                    fontPaint.Color = new SKColor(80, 73, 89);
                    fontPaint.Style = SKPaintStyle.Stroke;
                    fontPaint.StrokeWidth = 12;
                    mainCanvas.DrawText(pttStr, 130, 165, fontPaint);
                    fontPaint.Color = SKColors.White;
                    fontPaint.Style = SKPaintStyle.Fill;
                    mainCanvas.DrawText(pttStr, 130, 165, fontPaint);
                }
                else
                {
                    string pttInt = (accountInfo.Rating / 100).ToString() + ".";
                    string pttDec = (accountInfo.Rating % 100).ToString().PadLeft(2, '0');

                    fontPaint.TextAlign = SKTextAlign.Right;
                    fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/Exo-SemiBold.ttf"));
                    fontPaint.Color = new SKColor(80, 73, 89);
                    fontPaint.Style = SKPaintStyle.Stroke;
                    fontPaint.StrokeWidth = 12;
                    mainCanvas.DrawText(pttInt, 140, 160, fontPaint);
                    fontPaint.Color = SKColors.White;
                    fontPaint.Style = SKPaintStyle.Fill;
                    mainCanvas.DrawText(pttInt, 140, 160, fontPaint);

                    fontPaint.Color = new SKColor(80, 73, 89);
                    fontPaint.TextSize = 56;
                    fontPaint.TextAlign = SKTextAlign.Left;
                    fontPaint.Style = SKPaintStyle.Stroke;
                    fontPaint.StrokeWidth = 12;
                    mainCanvas.DrawText(pttDec, 140, 160, fontPaint);
                    fontPaint.Color = SKColors.White;
                    fontPaint.Style = SKPaintStyle.Fill;
                    mainCanvas.DrawText(pttDec, 140, 160, fontPaint);
                }
            }
            #endregion

            #region 绘制玩家姓名
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 72;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/GeosansLight.ttf"));

                mainCanvas.DrawText(accountInfo.UserName, 210, 145, fontPaint);
            }
            #endregion

            #region 绘制玩家代码
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 38;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/GeosansLight.ttf"));

                mainCanvas.DrawText(accountInfo.UserCode.ToString().Insert(6, " ").Insert(3, " "), 210, 192, fontPaint);
            }
            #endregion

            #region 绘制游玩时间
            DateTime playTime = songResult.TimePlayed;
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 48;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/ark-pixel-12px-latin.otf"));

                mainCanvas.DrawText(playTime.ToString("yyyy-MM-dd HH:mm:ss"), 50, 40, fontPaint);
            }
            #endregion

            #region 绘制Bot信息
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 48;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "font/ark-pixel-12px-latin.otf"));

                mainCanvas.DrawText("Generated By RinBot", 50, 990, fontPaint);
            }
            #endregion

            SKImage image = surface.Snapshot();
            byte[] data = image.Encode(SKEncodedImageFormat.Jpeg, 80).ToArray();
            surface.Dispose();
            image.Dispose();
            mainCanvas.Dispose();
            return data;
        }

        //public byte[] GeneratePttChart(List<PttRecord> records)
        //{
        //    SKImageInfo imageInfo = new SKImageInfo(1000, 500);
        //    SKSurface surface = SKSurface.Create(imageInfo);
        //    var canvas = surface.Canvas;
        //    var width = imageInfo.Width;
        //    var height = imageInfo.Height;

        //    var border = 50;

        //    float axisXLength = width - 2 * border;
        //    float axisYLength = height - 2 * border;

        //    using (SKPaint paint = new())
        //    {
        //        canvas.DrawLine(new SKPoint(border, height - border), new SKPoint(width - border, height - border), paint);

        //    }

        //    List<(DateTime, float)> lists = new();
        //    foreach (var x in records)
        //    {
        //        var date = DateTime.ParseExact(x.Date, "yyMMdd", CultureInfo.InvariantCulture);
        //        var score = float.Parse(x.PTT.ToString()) / 100;
        //        lists.Add(new(date, score));
        //    }

        //    List<SKPoint> points = new();


        //    var origin = new SKPoint(border, border + axisYLength);
        //    var path = new SKPath();
        //    path.MoveTo(origin);

        //    var minX = lists.Min(x => x.Item1);
        //    var deltaX = lists.Max(x => x.Item1) - minX;

        //    var minY = lists.Min(x => x.Item2);
        //    var maxY = lists.Max(x => x.Item2);

        //    float bottomY = (float)Math.Ceiling(maxY);
        //    float topY = (float)Math.Floor(minY);

        //    while (bottomY > minY)
        //        bottomY -= 0.5f;

        //    while (topY < maxY)
        //        topY += 0.5f;

        //    float deltaY = (float)(topY - bottomY);

        //    using (SKPaint linePaint = new())
        //    using (SKPaint fontPaint = new())
        //    {
        //        linePaint.Color = SKColors.LightGray;
        //        fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "fonts/NotoSansCJKtc-Regular.otf"));
        //        fontPaint.TextSize = 20;
        //        fontPaint.TextAlign = SKTextAlign.Right;
        //        fontPaint.IsAntialias = true;

        //        var delta = 0.5f;
        //        while (deltaY / delta > 5)
        //            delta += 0.5f;


        //        for (int i = 0; i <= deltaY / delta; i++)
        //        {
        //            var p0 = new SKPoint(border, border + axisYLength - (i * delta) * axisYLength / deltaY);
        //            var p1 = new SKPoint(border + axisXLength, border + axisYLength - (i * delta) * axisYLength / deltaY);
        //            canvas.DrawLine(p0, p1, linePaint);
        //            var str = (bottomY + i * delta).ToString("0.0");
        //            canvas.DrawText(str, p0.X - 8, p0.Y, fontPaint);
        //        }
        //    }

        //    using (SKPaint linePaint = new())
        //    using (SKPaint fontPaint = new())
        //    {
        //        linePaint.Color = SKColors.LightGray;
        //        fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "fonts/NotoSansCJKtc-Regular.otf"));
        //        fontPaint.TextSize = 20;
        //        fontPaint.TextAlign = SKTextAlign.Center;
        //        fontPaint.IsAntialias = true;

        //        var delta = TimeSpan.FromDays(30);
        //        while (deltaX / delta > 5)
        //            delta += TimeSpan.FromDays(30);


        //        for (int i = 0; i <= deltaX / delta; i++)
        //        {
        //            var p0 = new SKPoint((float)(border + (i * delta) * axisXLength / deltaX), border + axisYLength);
        //            var p1 = new SKPoint((float)(border + (i * delta) * axisXLength / deltaX), border + axisYLength - 10);
        //            canvas.DrawLine(p0, p1, linePaint);
        //            var str = (minX + i * delta).ToString("yyyy/MM");
        //            canvas.DrawText(str, p1.X, p1.Y + 40, fontPaint);
        //        }
        //    }

        //    foreach (var (a, b) in lists)
        //    {
        //        float x = (float)((a - minX) / deltaX * axisXLength);
        //        float y = (b - bottomY) / deltaY * axisYLength;
        //        points.Add(new SKPoint(origin.X + x, origin.Y - y));
        //    }

        //    if (points.Count == 1)
        //    {
        //        var p = points[0];
        //        path.MoveTo(border, p.Y);
        //        path.LineTo(border + axisXLength, p.Y);
        //    }
        //    else
        //    {
        //        for (int i = 0; i < points.Count; i++)
        //        {
        //            var p = points[i];
        //            if (i == 0)
        //            {
        //                path.MoveTo(p);
        //            }
        //            else
        //            {
        //                path.LineTo(p);
        //            }
        //        }
        //    }
        //    using (SKPaint pathPaint = new SKPaint())
        //    {
        //        pathPaint.Style = SKPaintStyle.Stroke;
        //        pathPaint.StrokeWidth = 4;
        //        pathPaint.Color = SKColors.SkyBlue;
        //        pathPaint.IsAntialias = true;
        //        canvas.DrawPath(path, pathPaint);
        //    }

        //    using (SKPaint fontPaint = new())
        //    {
        //        fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(ARCAEA_RESOURCE_PATH, "fonts/NotoSansCJKtc-Regular.otf"));
        //        fontPaint.TextSize = 20;
        //        fontPaint.TextAlign = SKTextAlign.Center;
        //        fontPaint.IsAntialias = true;

        //        var point = path.LastPoint;
        //        var str = ((float)records.Last().PTT / 100).ToString("0.00");
        //        canvas.DrawText(str, point.X, point.Y - 10, fontPaint);
        //    }
        //    var bytes = surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).ToArray();
        //    canvas.Dispose();
        //    surface.Dispose();
        //    return bytes;
        //}

        private string FormatScore(int score)
        {
            return score.ToString().PadLeft(8, '0').Insert(5, "\'").Insert(2, "\'");
        }

        public byte[]? GetSongCover(Chart chart)
        {
            string path = Path.Combine(ArcaeaModule.COVER_DIR_PATH, $"{chart.SongId}_{((int)chart.RatingClass)}.jpg");
            if (chart.RatingClass == RatingClass.Past)
            {
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
                else
                {
                    var bytes = ArcaeaModule.ArcaeaUnlimitedAPI.GetSongCover(chart.SongId, chart.RatingClass).Result;
                    File.WriteAllBytesAsync(path, bytes);
                    return bytes;
                }
            }
            else
            {
                if (chart.JacketOverride)
                    if (File.Exists(path))
                        return File.ReadAllBytes(path);
                    else
                    {
                        var bytes = ArcaeaModule.ArcaeaUnlimitedAPI.GetSongCover(chart.SongId, chart.RatingClass).Result;
                        File.WriteAllBytesAsync(path, bytes);
                        return bytes;
                    }
                else
                {
                    chart.RatingClass--;
                    return GetSongCover(chart);
                }
            }
        }

        private SKBitmap GetCharaImg(uint chara, bool isUncapped = false)
        {
            if (!File.Exists(Path.Combine(ARCAEA_RESOURCE_PATH, $"chara/{chara}{(isUncapped ? "u" : "")}.png")))
            {
                File.WriteAllBytes(Path.Combine(ARCAEA_RESOURCE_PATH, $"chara/{chara}{(isUncapped ? "u" : "")}.png"), ArcaeaModule.ArcaeaUnlimitedAPI.GetCharaIllust(chara, isUncapped).Result);
            }

            return SKBitmap.Decode(Path.Combine(ARCAEA_RESOURCE_PATH, $"chara/{chara}{(isUncapped ? "u" : "")}.png"));
        }

        private SKBitmap GetCharaIcon(uint chara, bool isUncapped = false)
        {
            if (!File.Exists(Path.Combine(ARCAEA_RESOURCE_PATH, $"chara/{chara}{(isUncapped ? "u" : "")}_icon.png")))
            {
                File.WriteAllBytes(Path.Combine(ARCAEA_RESOURCE_PATH, $"chara/{chara}{(isUncapped ? "u" : "")}_icon.png"), ArcaeaModule.ArcaeaUnlimitedAPI.GetCharaIcon(chara, isUncapped).Result);
            }

            return SKBitmap.Decode(Path.Combine(ARCAEA_RESOURCE_PATH, $"chara/{chara}{(isUncapped ? "u" : "")}_icon.png"));
        }

        private SKColor GetAverageColor(SKBitmap bitmap)
        {
            var resized = new SKBitmap(1, 1);
            bitmap.ScalePixels(resized, SKFilterQuality.High);
            var color = resized.GetPixel(0, 0);

            return new SKColor((byte)(color.Red / 1.5), (byte)(color.Green / 1.5), (byte)(color.Blue / 1.5));
        }
    }
}
