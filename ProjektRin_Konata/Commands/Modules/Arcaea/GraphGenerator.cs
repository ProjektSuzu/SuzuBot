using ProjektRin.Components;
using SkiaSharp;

namespace ProjektRin.Commands.Modules.Arcaea
{
    internal static class GraphGenerator
    {
        private static readonly string resourcePath = Path.Combine(BotManager.resourcePath, "ArcaeaProbe_Skia");

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

        public static byte[] GenerateSingleSong(SongResult songResult, int id = 1)
        {
            //准备工作
            SKImageInfo imageInfo = new(1360, 600);
            SKSurface surface = SKSurface.Create(imageInfo);
            SKCanvas mainCanvas = surface.Canvas;
            //Console.WriteLine($"ID: {id}  {songResult.song_id}");

            #region 获取歌曲信息
            Song song = ArcSongDB.Instance.GetSong(songResult.song_id);
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
            SKBitmap bitmap = GetCoverBmp(songResult.song_id);
            SKBitmap scaledBitmap = new SKBitmap(580, 580);
            bitmap.ScalePixels(scaledBitmap, SKFilterQuality.None);
            #endregion

            #region 获取封面图片的平均色
            SKColor averageColor = GetAverageColor(bitmap);
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
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/NotoSansCJKtc-Regular.otf"));
                SKRect rect = new SKRect();
                float maxWidth = 700;
                fontPaint.MeasureText(songName, ref rect);
                while (rect.Size.Width > maxWidth)
                {
                    songName = songName.Substring(0, songName.Length - 3) + "..";
                    fontPaint.MeasureText(songName, ref rect);
                }

                mainCanvas.DrawText(songName, 970, 180, fontPaint);
            }
            #endregion

            #region 绘制难度信息条
            using (SKPaint bgPaint = new SKPaint())
            {
                bgPaint.Color = difficultyColorsDarker[(int)songResult.difficulty];
                SKRect backDifficultyBorad = new SKRect(10, 10, 1350, 110);
                SKRoundRect backDifficultyBoradRound = new SKRoundRect(backDifficultyBorad, 50);
                mainCanvas.DrawRoundRect(backDifficultyBoradRound, bgPaint);

                bgPaint.Color = difficultyColors[(int)songResult.difficulty];
                SKRect frontDifficultyBorad = new SKRect(10, 10, 860, 110);
                SKRoundRect frontDifficultyBoradRound = new SKRoundRect(frontDifficultyBorad, 50);
                mainCanvas.DrawRoundRect(frontDifficultyBoradRound, bgPaint);
            }
            #endregion

            #region 绘制歌曲PTT
            string songPTT = $"{songResult.rating:0.0000}";
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-Regular.ttf"));
                SKRect rect = new SKRect();
                fontPaint.MeasureText(songPTT, ref rect);
                float w = rect.Size.Width;
                float h = rect.Size.Height;

                mainCanvas.DrawText(songPTT, 710, 80, fontPaint);
            }
            #endregion

            #region 绘制歌曲定数
            string songRating = "WTF?!";
            switch (songResult.difficulty)
            {
                case SongResult.Difficulty.Beyond:
                    songRating = $"BEYOND {(float)song.RatingBYD / 10:0.00}";
                    break;

                case SongResult.Difficulty.Future:
                    songRating = $"FUTURE {(float)song.RatingFTR / 10:0.00}";
                    break;

                case SongResult.Difficulty.Present:
                    songRating = $"PRESENT {(float)song.RatingPRS / 10:0.00}";
                    break;

                case SongResult.Difficulty.Past:
                    songRating = $"PAST {(float)song.RatingPST / 10:0.00}";
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
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-Regular.ttf"));
                SKRect rect = new SKRect();
                fontPaint.MeasureText(songRating, ref rect);
                float w = rect.Size.Width;
                float h = rect.Size.Height;

                mainCanvas.DrawText(songRating, 1080, 80, fontPaint);
            }
            #endregion

            #region 绘制歌曲分数
            string songScore = FormatScore(songResult.score);
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = textColor;
                fontPaint.TextSize = 128;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-Regular.ttf"));

                mainCanvas.DrawText(songScore, 970, 300, fontPaint);
            }
            #endregion

            #region 绘制PFL条
            int total_count = songResult.miss_count + songResult.near_count + songResult.perfect_count;
            using (SKPaint bgPaint = new SKPaint())
            {
                //Lost
                bgPaint.Color = SKColors.LimeGreen;
                SKRect lostRect = new SKRect(620, 420, 1320, 440);
                SKRoundRect lostRectRound = new SKRoundRect(lostRect, 10);
                mainCanvas.DrawRoundRect(lostRectRound, bgPaint);

                //Far
                bgPaint.Color = SKColors.Yellow;
                SKRect farRect = new SKRect(620, 420, 620 + (float)(songResult.near_count + songResult.perfect_count) / total_count * 700, 440);
                SKRoundRect farRectRound = new SKRoundRect(farRect, 10);
                mainCanvas.DrawRoundRect(farRectRound, bgPaint);

                //Pure
                bgPaint.Color = SKColors.DodgerBlue;
                SKRect pureRect = new SKRect(620, 420, 620 + (float)songResult.perfect_count / total_count * 700, 440);
                SKRoundRect pureRectRound = new SKRoundRect(pureRect, 10);
                mainCanvas.DrawRoundRect(pureRectRound, bgPaint);

                //MaxPure
                bgPaint.Color = SKColors.Violet;
                SKRect maxPureRect = new SKRect(620, 420, 620 + (float)songResult.shiny_perfect_count / total_count * 700, 440);
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
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-Regular.ttf"));

                //Pure
                string pureText = $"P:{songResult.perfect_count}";
                float pureTextOffset = (float)songResult.perfect_count / total_count * 700;
                if (pureTextOffset < 200)
                {
                    pureTextOffset = 200;
                }

                mainCanvas.DrawText(pureText, 620 + pureTextOffset / 2, 400, fontPaint);

                fontPaint.Color = SKColors.Violet;
                fontPaint.TextAlign = SKTextAlign.Left;
                string maxPureText = $"[{songResult.shiny_perfect_count}]";
                mainCanvas.DrawText(maxPureText, 620 + pureTextOffset / 2, 400, fontPaint);

                //Far
                fontPaint.Color = SKColors.Yellow;
                fontPaint.TextAlign = SKTextAlign.Right;
                string farText = $"F:{songResult.near_count}";
                float farTextOffset = (float)songResult.near_count / total_count * 700 + pureTextOffset;
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
                string lostText = $"L:{songResult.miss_count}";
                float lostTextOffset = (float)songResult.miss_count / total_count * 700;

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
            #endregion

            #region 绘制游玩时间和序号
            DateTime playTime = TimeZoneInfo
                .ConvertTimeFromUtc(new DateTime(1970, 1, 1), TimeZoneInfo.Local)
                .AddMilliseconds(songResult.time_played);
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.Black;
                fontPaint.TextSize = 60;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/GeosansLight-Oblique.ttf"));

                mainCanvas.DrawText(playTime.ToString("G"), 600, 585, fontPaint);

                fontPaint.TextAlign = SKTextAlign.Right;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-Regular.ttf"));

                string idStr = $"#{id.ToString().PadLeft(2, '0')}";
                mainCanvas.DrawText(idStr, 1320, 585, fontPaint);
            }
            #endregion

            #region 导出图片字节
            SKImage image = surface.Snapshot();
            SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            //File.WriteAllBytes("resources/test.png", data.ToArray());
            return data.ToArray();
            #endregion
        }

        public static byte[] GenerateBest30(B30Result b30Result)
        {
            //准备工作
            SKImageInfo imageInfo = new SKImageInfo(2400, 5000);
            SKSurface surface = SKSurface.Create(imageInfo);
            SKCanvas mainCanvas = surface.Canvas;

            #region 绘制背景
            using (SKPaint bgPaint = new SKPaint())
            {
                SKBitmap background = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(resourcePath, "background.jpg")));
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
            AccountInfo playerInfo = b30Result.content.account_Info;
            #endregion

            #region 绘制玩家信息框
            using (SKPaint bgPaint = new SKPaint())
            {
                SKBitmap bmp = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(resourcePath, "user_back.png")));
                SKBitmap scaledBitmap = new SKBitmap(900, 250);
                bmp.ScalePixels(scaledBitmap, SKFilterQuality.None);

                mainCanvas.DrawBitmap(scaledBitmap, 345, 160);
            }
            #endregion

            #region 绘制搭档图标
            using (SKPaint bgPaint = new SKPaint())
            {
                SKBitmap bmp = GetCharaIcon((uint)playerInfo.character, playerInfo.is_char_uncapped);
                SKBitmap scaledBitmap = new SKBitmap(400, 400);
                bmp.ScalePixels(scaledBitmap, SKFilterQuality.Medium);

                mainCanvas.DrawBitmap(scaledBitmap, 50, 50);
            }
            #endregion

            #region 绘制PTT边框
            using (SKPaint bgPaint = new SKPaint())
            {
                SKBitmap bmp = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(resourcePath, $"ranks/rank_{playerInfo.GetPlayerPTTType()}.png")));
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
                fontPaint.TextAlign = SKTextAlign.Right;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-SemiBold.ttf"));

                string pttInt = (playerInfo.rating / 100).ToString() + ".";
                string pttDec = (playerInfo.rating % 100).ToString().PadRight(2, '0');

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
            #endregion

            #region 绘制玩家姓名
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 144;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/GeosansLight.ttf"));

                mainCanvas.DrawText(playerInfo.name, 460, 300, fontPaint);
            }
            #endregion

            #region 绘制玩家代码
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/GeosansLight.ttf"));

                mainCanvas.DrawText(playerInfo.code.ToString().Insert(6, " ").Insert(3, " "), 550, 400, fontPaint);
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
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/DottedSongtiSquareRegular.otf"));

                mainCanvas.DrawText("Generated By ProjektRin", 50, 550, fontPaint);
            }
            #endregion

            #region 绘制生成时间
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Right;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/DottedSongtiSquareRegular.otf"));

                mainCanvas.DrawText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 2350, 550, fontPaint);
            }
            #endregion

            #region 绘制B30分割板和标题
            using (SKPaint fontPaint = new SKPaint())
            {
                SKBitmap bitmap = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(resourcePath, "spliter.png")));
                SKBitmap scaledBitmap = new SKBitmap(630, 126);
                bitmap.ScalePixels(scaledBitmap, SKFilterQuality.Medium);

                mainCanvas.DrawBitmap(scaledBitmap, 1200 - scaledBitmap.Width / 2, 525);


                fontPaint.TextSize = 96;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-Regular.ttf"));

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
            }
            for (int i = 0; i < b30Result.content.best30_list.Count; i++)
            {
                int id = i;
                Task? task = new Task(() =>
                {
                    SKBitmap singleSongBmp = SKBitmap.Decode(GenerateSingleSong(b30Result.content.best30_list[id], id + 1));
                    SKBitmap scaledBitmap = new SKBitmap(680, 300);
                    singleSongBmp.ScalePixels(scaledBitmap, SKFilterQuality.Medium);
                    GenerateCallback(scaledBitmap, id);
                });
                tasks.Add(task);
                task.Start();
            }
            Task.WaitAll(tasks.ToArray());
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
                SKBitmap bitmap = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(resourcePath, "spliter.png")));
                SKBitmap scaledBitmap = new SKBitmap(630, 126);
                bitmap.ScalePixels(scaledBitmap, SKFilterQuality.Medium);

                mainCanvas.DrawBitmap(scaledBitmap, 1200 - scaledBitmap.Width / 2, 4475);


                fontPaint.TextSize = 96;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-Regular.ttf"));

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
            if (b30Result.content.best30_overflow.Count > 0)
            {
                for (int i = 0; i < b30Result.content.best30_overflow.Count; i++)
                {
                    SKBitmap singleSongBmp = SKBitmap.Decode(GenerateSingleSong(b30Result.content.best30_overflow[i], i + 31));
                    SKBitmap scaledBitmap = new SKBitmap(680, 300);
                    singleSongBmp.ScalePixels(scaledBitmap, SKFilterQuality.Medium);

                    int row = i / 3;
                    int col = i % 3;

                    mainCanvas.DrawBitmap(scaledBitmap, 1200 - 340 + (col - 1) * 770, 4640);
                }
            }
            else
            {
                using (SKPaint fontPaint = new SKPaint())
                {
                    fontPaint.TextSize = 128;
                    fontPaint.IsAntialias = true;
                    fontPaint.TextAlign = SKTextAlign.Center;
                    fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-Regular.ttf"));

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
            string b30AVG = b30Result.content.best30_avg.ToString("0.0000");
            string r10AVG = b30Result.content.recent10_avg.ToString("0.0000");
            string maxEST = ((b30Result.content.best30_avg + b30Result.content.recent10_avg) / 2).ToString("0.0000");
            using (SKPaint fontPaint = new SKPaint())
            {
                SKBitmap infoBorad = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(resourcePath, "res_scoresection_beyond.png")));
                SKBitmap scaledBitmap = new SKBitmap(1000, 500);
                infoBorad.ScalePixels(scaledBitmap, SKFilterQuality.None);
                mainCanvas.DrawBitmap(scaledBitmap, 2400 - scaledBitmap.Width, 0);

                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 84;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Right;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-Regular.ttf"));

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
            SKBitmap scaledImage = new SKBitmap((int)(image.Width * 0.75), (int)(image.Height * 0.75));
            image.ScalePixels(scaledImage, SKFilterQuality.Medium);
            SKData data = scaledImage.Encode(SKEncodedImageFormat.Jpeg, 80);
            //File.WriteAllBytes("resources/test.jpg", data.ToArray());
            return data.ToArray();
        }

        public static byte[] GeneratePlayResult(PlayResult playResult)
        {
            //准备工作
            SKImageInfo imageInfo = new SKImageInfo(1800, 1000);
            SKSurface surface = SKSurface.Create(imageInfo);
            SKCanvas mainCanvas = surface.Canvas;

            #region 获取游玩数据
            AccountInfo accountInfo;
            SongResult songResult;
            Song songInfo;
            if (playResult is UserInfoResult)
            {
                accountInfo = ((UserInfoResult)playResult).content.account_info;
                songResult = ((UserInfoResult)playResult).content.recent_score.First();
            }
            else
            {
                accountInfo = ((BestPlayResult)playResult).content.account_info;
                songResult = ((BestPlayResult)playResult).content.record;
            }
            songInfo = ArcSongDB.Instance.GetSong(songResult.song_id);
            #endregion

            #region 绘制背景
            using (SKPaint bgPaint = new())
            {
                SKBitmap background = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(resourcePath, "beyond.png")));
                bgPaint.ImageFilter = SKImageFilter.CreateBlur(10, 10);
                bgPaint.IsAntialias = true;
                mainCanvas.DrawBitmap(background, 0, 0, bgPaint);

                bgPaint.ImageFilter = null;
                bgPaint.Color = new SKColor(0, 0, 0, 64);
                SKRect rect = new SKRect(0, 0, 1800, 1000);
                mainCanvas.DrawRect(rect, bgPaint);
            }
            #endregion

            #region 获取封面图片
            SKBitmap bitmap = GetCoverBmp(songResult.song_id);
            SKBitmap scaledBitmap = new SKBitmap(550, 550);
            bitmap.ScalePixels(scaledBitmap, SKFilterQuality.None);
            #endregion

            #region 获取封面图片的平均色
            SKColor averageColor = GetAverageColor(bitmap);
            #endregion

            #region 根据平均色决定文字颜色
            SKColor textColor = SKColors.White;
            int Luminance = (int)(0.2126 * averageColor.Red + 0.7152 * averageColor.Green + 0.0722 * averageColor.Blue);
            Console.WriteLine(songResult.song_id + " " + Luminance);
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
                bgPaint.Color = difficultyColors[(int)songResult.difficulty];
                SKRect backBoardDown = new SKRect(70, 220, 1730, 340);
                SKRoundRect backBoardDownRound = new SKRoundRect(backBoardDown, 50);
                mainCanvas.DrawRoundRect(backBoardDownRound, bgPaint);

                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 96;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.TextEncoding = SKTextEncoding.Utf8;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/NotoSansCJKsc-Regular.otf"));
                SKRect rect = new SKRect();
                float maxWidth = 1600;
                fontPaint.MeasureText(songName, ref rect);
                while (rect.Size.Width > maxWidth)
                {
                    songName = songName.Substring(0, songName.Length - 3) + "..";
                    fontPaint.MeasureText(songName, ref rect);
                }

                mainCanvas.DrawText(songName, 900, 310, fontPaint);
            }
            #endregion

            #region 绘制搭档立绘
            using (SKBitmap chara = GetCharaImg((uint)accountInfo.character, accountInfo.is_char_uncapped))
            {
                SKBitmap scaledCharaBitmap = new SKBitmap(1500, 1500);
                chara.ScalePixels(scaledCharaBitmap, SKFilterQuality.Low);

                mainCanvas.DrawBitmap(scaledCharaBitmap, 800, 0);
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
            #endregion

            #region 绘制歌曲定数和PTT
            string songRating = "WTF?!";
            switch (songResult.difficulty)
            {
                case SongResult.Difficulty.Beyond:
                    songRating = $"{(float)songInfo.RatingBYD / 10:0.00}";
                    break;

                case SongResult.Difficulty.Future:
                    songRating = $"{(float)songInfo.RatingFTR / 10:0.00}";
                    break;

                case SongResult.Difficulty.Present:
                    songRating = $"{(float)songInfo.RatingPRS / 10:0.00}";
                    break;

                case SongResult.Difficulty.Past:
                    songRating = $"{(float)songInfo.RatingPST / 10:0.00}";
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
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-Regular.ttf"));

                mainCanvas.DrawText($"PTT {songRating} => {songResult.rating.ToString("0.0000")}", 965, 640, fontPaint);
            }
            #endregion

            #region 绘制歌曲得分进度
            int progress = 0;
            int theoretical = songInfo.GetTheoreticalValue(songResult.difficulty);
            float percentage;
            int diff;
            string progressStr;
            switch (songResult.GetClearType())
            {
                case SongResult.ClearType.PM:
                    progress = 3;
                    diff = theoretical - songResult.score;
                    percentage = (float)diff / (theoretical - 10_000_000);
                    progressStr = $"TV  -{FormatScore(diff)}";
                    break;

                case SongResult.ClearType.EXPlus:
                    progress = 3;
                    diff = 10_000_000 - songResult.score;
                    percentage = (float)diff / 00_100_000;
                    progressStr = $"PM  -{FormatScore(diff)}";
                    break;

                case SongResult.ClearType.EX:
                    progress = 3;
                    diff = 09_900_000 - songResult.score;
                    percentage = (float)diff / 00_100_000;
                    progressStr = $"EX+  -{FormatScore(diff)}";
                    break;

                case SongResult.ClearType.AA:
                    progress = 2;
                    diff = 09_800_000 - songResult.score;
                    percentage = (float)diff / 00_300_000;
                    progressStr = $"EX  -{FormatScore(diff)}";
                    break;

                case SongResult.ClearType.A:
                    progress = 2;
                    diff = 09_500_000 - songResult.score;
                    percentage = (float)diff / 00_300_000;
                    progressStr = $"AA  -{FormatScore(diff)}";
                    break;

                case SongResult.ClearType.B:
                    progress = 1;
                    diff = 09_200_000 - songResult.score;
                    percentage = (float)diff / 00_300_000;
                    progressStr = $"A  -{FormatScore(diff)}";
                    break;

                case SongResult.ClearType.C:
                    progress = 1;
                    diff = 08_900_000 - songResult.score;
                    percentage = (float)diff / 00_300_000;
                    progressStr = $"B  -{FormatScore(diff)}";
                    break;

                default:
                    progress = 1;
                    diff = 08_600_000 - songResult.score;
                    percentage = (float)diff / 00_300_000;
                    progressStr = $"C  -{FormatScore(diff)}";
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
                SKRect progressBarFront = new SKRect(670, 500, 670 + 660 * percentage, 550);
                SKRoundRect progressBarRoundFront = new SKRoundRect(progressBarFront, 25);
                mainCanvas.DrawRoundRect(progressBarRoundFront, bgPaint);

                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 56;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.TextEncoding = SKTextEncoding.Utf8;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-Regular.ttf"));

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
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-Regular.ttf"));

                mainCanvas.DrawText(FormatScore(songResult.score), 965, 480, fontPaint);
            }
            #endregion

            #region 绘制PFL条
            int total_count = songResult.miss_count + songResult.near_count + songResult.perfect_count;
            using (SKPaint bgPaint = new SKPaint())
            {
                //Lost
                bgPaint.Color = SKColors.LimeGreen;
                SKRect lostRect = new SKRect(670, 800, 1270, 850);
                SKRoundRect lostRectRound = new SKRoundRect(lostRect, 25);
                mainCanvas.DrawRoundRect(lostRectRound, bgPaint);

                //Far
                bgPaint.Color = SKColors.Yellow;
                SKRect farRect = new SKRect(670, 800, 670 + (float)(songResult.near_count + songResult.perfect_count) / total_count * 600, 850);
                SKRoundRect farRectRound = new SKRoundRect(farRect, 25);
                mainCanvas.DrawRoundRect(farRectRound, bgPaint);

                //Pure
                bgPaint.Color = SKColors.DodgerBlue;
                SKRect pureRect = new SKRect(670, 800, 670 + (float)songResult.perfect_count / total_count * 600, 850);
                SKRoundRect pureRectRound = new SKRoundRect(pureRect, 25);
                mainCanvas.DrawRoundRect(pureRectRound, bgPaint);

                //MaxPure
                bgPaint.Color = SKColors.Violet;
                SKRect maxPureRect = new SKRect(670, 800, 670 + (float)songResult.shiny_perfect_count / total_count * 600, 850);
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
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-Regular.ttf"));

                //Pure
                string pureText = $"P:{songResult.perfect_count}";
                float pureTextOffset = (float)songResult.perfect_count / total_count * 600;
                if (pureTextOffset < 200)
                {
                    pureTextOffset = 200;
                }

                mainCanvas.DrawText(pureText, 670 + pureTextOffset / 2, 780, fontPaint);

                fontPaint.Color = SKColors.Violet;
                fontPaint.TextAlign = SKTextAlign.Left;
                string maxPureText = $"[{songResult.shiny_perfect_count}]";
                mainCanvas.DrawText(maxPureText, 670 + pureTextOffset / 2, 780, fontPaint);

                //Far
                fontPaint.Color = SKColors.Yellow;
                fontPaint.TextAlign = SKTextAlign.Right;
                string farText = $"F:{songResult.near_count}";
                float farTextOffset = (float)songResult.near_count / total_count * 600 + pureTextOffset;
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
                string lostText = $"L:{songResult.miss_count}";
                float lostTextOffset = (float)songResult.miss_count / total_count * 600;

                mainCanvas.DrawText(lostText, 1320 - lostTextOffset / 2, 780, fontPaint);
            }
            #endregion

            #region 绘制玩家信息框
            using (SKBitmap userBack = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(resourcePath, "user_back.png"))))
            {
                SKBitmap scaledUserBack = new SKBitmap(400, 115);
                userBack.ScalePixels(scaledUserBack, SKFilterQuality.Low);

                mainCanvas.DrawBitmap(scaledUserBack, 125, 80);
            }
            #endregion

            #region 绘制PTT边框
            using (SKPaint bgPaint = new SKPaint())
            {
                SKBitmap pttBmp = SKBitmap.Decode(File.ReadAllBytes(Path.Combine(resourcePath, $"ranks/rank_{accountInfo.GetPlayerPTTType()}.png")));
                SKBitmap scaledPTTBmp = new SKBitmap(160, 160);
                pttBmp.ScalePixels(scaledPTTBmp, SKFilterQuality.Low);

                mainCanvas.DrawBitmap(scaledPTTBmp, 50, 60);
            }
            #endregion

            #region 绘制PTT
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.TextSize = 84;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Right;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/Exo-SemiBold.ttf"));

                string pttInt = (accountInfo.rating / 100).ToString() + ".";
                string pttDec = (accountInfo.rating % 100).ToString().PadRight(2, '0');

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
            #endregion

            #region 绘制玩家姓名
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 72;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/GeosansLight.ttf"));

                mainCanvas.DrawText(accountInfo.name, 210, 145, fontPaint);
            }
            #endregion

            #region 绘制玩家代码
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 38;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/GeosansLight.ttf"));

                mainCanvas.DrawText(accountInfo.code.ToString().Insert(6, " ").Insert(3, " "), 210, 192, fontPaint);
            }
            #endregion

            #region 绘制游玩时间
            DateTime playTime = TimeZoneInfo
                .ConvertTimeFromUtc(new DateTime(1970, 1, 1), TimeZoneInfo.Local)
                .AddMilliseconds(songResult.time_played);
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 64;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Center;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/GeosansLight-Oblique.ttf"));

                mainCanvas.DrawText(playTime.ToString("yyyy-MM-dd HH:mm:ss"), 900, 150, fontPaint);
            }
            #endregion

            #region 绘制Bot信息
            using (SKPaint fontPaint = new SKPaint())
            {
                fontPaint.Color = SKColors.White;
                fontPaint.TextSize = 48;
                fontPaint.IsAntialias = true;
                fontPaint.TextAlign = SKTextAlign.Left;
                fontPaint.Typeface = SKTypeface.FromFile(Path.Combine(resourcePath, "fonts/DottedSongtiSquareRegular.otf"));

                mainCanvas.DrawText("Generated By ProjektRin", 50, 980, fontPaint);
            }
            #endregion

            SKImage image = surface.Snapshot();
            SKData data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
            return data.ToArray();
        }

        private static string FormatScore(int score)
        {
            return score.ToString().PadLeft(8, '0').Insert(5, "\'").Insert(2, "\'");
        }

        public static SKBitmap GetCoverBmp(string sid, bool beyond = false)
        {
            string path = Path.Combine(resourcePath, $"covers/{sid}{(beyond ? " byd" : "")}.jpg");
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, ArcaeaUnlimitedAPI.Instance.GetSongCover(sid, beyond).Result);
            }

            return SKBitmap.Decode(path);
        }

        private static SKBitmap GetCharaImg(uint chara, bool isUncapped = false)
        {
            if (!File.Exists(Path.Combine(resourcePath, $"charas/{chara}{(isUncapped ? "u" : "")}.png")))
            {
                File.WriteAllBytes(Path.Combine(resourcePath, $"charas/{chara}{(isUncapped ? "u" : "")}.png"), ArcaeaUnlimitedAPI.Instance.GetCharaImg(chara, isUncapped).Result);
            }

            return SKBitmap.Decode(Path.Combine(resourcePath, $"charas/{chara}{(isUncapped ? "u" : "")}.png"));
        }

        private static SKBitmap GetCharaIcon(uint chara, bool isUncapped = false)
        {
            if (!File.Exists(Path.Combine(resourcePath, $"charas/{chara}{(isUncapped ? "u" : "")}_icon.png")))
            {
                File.WriteAllBytes(Path.Combine(resourcePath, $"charas/{chara}{(isUncapped ? "u" : "")}_icon.png"), ArcaeaUnlimitedAPI.Instance.GetCharaIcon(chara, isUncapped).Result);
            }

            return SKBitmap.Decode(Path.Combine(resourcePath, $"charas/{chara}{(isUncapped ? "u" : "")}_icon.png"));
        }

        private static SKColor GetAverageColor(SKBitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            int[] pixels = new int[width * height];

            int r = 0, g = 0, b = 0;
            foreach (SKColor pixel in bitmap.Pixels)
            {
                if (pixel.Red == 0 && pixel.Green == 0 && pixel.Blue == 0)
                {
                    continue;
                }

                r += pixel.Red & 0xff;
                g += pixel.Green & 0xff;
                b += pixel.Blue & 0xff;
            }

            r /= pixels.Length;
            g /= pixels.Length;
            b /= pixels.Length;

            return new SKColor((byte)r, (byte)g, (byte)b);
        }
    }
}
