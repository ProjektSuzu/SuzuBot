using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RinBot.Core.Components;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace RinBot.Commands.Modules.Apex
{
    internal class ApexAPI
    {
        private static readonly string configPath = Path.Combine(BotManager.resourcePath, "ApexProbe/config.json");
        private static readonly string mapCoverDir = Path.Combine(BotManager.resourcePath, "ApexProbe/covers");
        private ApexConfig config;

        #region 单例模式
        private static ApexAPI instance;
        public static ApexAPI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ApexAPI();
                }
                return instance;
            }
        }
        private ApexAPI()
        {
            if (File.Exists(configPath))
            {
                config = JsonConvert.DeserializeObject<ApexConfig>(File.ReadAllText(configPath));
            }
        }
        #endregion

        HttpClient httpClient = new HttpClient()
        {
            Timeout = new TimeSpan(0, 0, 15)
        };

        private static readonly string statsApi =
            @"https://api.mozambiquehe.re/bridge?version=5&platform=PC&player={player}&auth={token}";
        private static readonly string N2UApi =
            @"https://api.mozambiquehe.re/nametouid?player={player}&platform=PC&auth={token}";
        private static readonly string predatorApi =
            @"https://api.mozambiquehe.re/predator?auth={token}";
        private static readonly string mapRotationApi =
            @"https://api.mozambiquehe.re/maprotation?auth={token}&version=2";

        private static readonly string TAG = "APEX";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public string GetMapNameCN(string name)
        {
            switch (name)
            {
                case "Kings Canyon": return "诸王峡谷";
                case "World's Edge": return "世界尽头";
                case "Storm Point": return "风暴点";
                case "Olympus": return "奥林匹斯";
                case "Party crasher": return "派对破坏者";
                case "Encore": return "再来一次";
                case "Overflow": return "熔岩流";
                case "Drop Off": return "原料厂";
                case "Habitat": return "栖息地 4";

                default: return name;
            }
        }

        public async Task<PlayerStats?> GetPlayerStats(string userId)
        {
            var url = statsApi.Replace("{player}", userId).Replace("{token}", config.Token);
            var response = httpClient.GetAsync(url).Result;
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PlayerStats>(responseString);

        }

        public async Task<UidInfo> GetPlayerUid(string userName)
        {
            var url = N2UApi.Replace("{player}", userName).Replace("{token}", config.Token);
            var response = httpClient.GetAsync(url).Result;
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<UidInfo>(responseString);
        }

        public async Task<PredatorInfo> GetPredatorInfo()
        {
            var url = predatorApi.Replace("{token}", config.Token);
            var response = httpClient.GetAsync(url).Result;
            var responseString = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<JObject>(responseString);
            var rp = json["RP"]["PC"]["val"].Value<uint>();
            var ap = json["AP"]["PC"]["val"].Value<uint>();

            return new()
            {
                RankPoint = rp,
                ArenaPoint = ap
            };
        }

        public byte[] GetMapCover(ApexMapRotation.Mode.Info map)
        {
            if (!File.Exists(Path.Combine(mapCoverDir, $"{map.code}.png")))
            {
                HttpClient httpClient = new();
                File.WriteAllBytes(Path.Combine(mapCoverDir, $"{map.code}.png"), httpClient.GetByteArrayAsync(map.asset).Result);
            }
            return File.ReadAllBytes(Path.Combine(mapCoverDir, $"{map.code}.png"));
        }

        public byte[] GetMapRotationImg()
        {
            HttpClient httpClient = new();
            var url = mapRotationApi.Replace("{token}", config.Token);
            var response = httpClient.GetAsync(url).Result;
            var rotation = JsonConvert.DeserializeObject<ApexMapRotation>(response.Content.ReadAsStringAsync().Result);

            SKImageInfo imageInfo = new SKImageInfo(1920, 2400);
            SKSurface surface = SKSurface.Create(imageInfo);
            SKCanvas canvas = surface.Canvas;

            var font = SKTypeface.FromFile(Path.Combine(BotManager.resourcePath, "ApexProbe/fusion-pixel.otf"));

            var battleRoyaleMapCoverCurrent = SKBitmap.Decode(GetMapCover(rotation.battle_royale.current));
            //var battleRoyaleMapCoverNext = SKBitmap.Decode(httpClient.GetAsync(rotation.battle_royale.next.asset).Result.Content.ReadAsStream());
            var battleRoyaleRankedMapCoverCurrent = SKBitmap.Decode(GetMapCover(rotation.ranked.current));
            //var battleRoyaleRankedMapCoverNext = SKBitmap.Decode(httpClient.GetAsync(rotation.ranked.next.asset).Result.Content.ReadAsStream());
            var arenaMapCoverCurrent = SKBitmap.Decode(GetMapCover(rotation.arenas.current));
            //var arenaMapCoverNext = SKBitmap.Decode(httpClient.GetAsync(rotation.arenas.next.asset).Result.Content.ReadAsStream());
            var arenaRankedMapCoverCurrent = SKBitmap.Decode(GetMapCover(rotation.arenasRanked.current));
            //var arenaRankedMapCoverNext = SKBitmap.Decode(httpClient.GetAsync(rotation.arenasRanked.next.asset).Result.Content.ReadAsStream());
            using (SKPaint paint = new())
            {
                //paint.ImageFilter = SKImageFilter.CreateBlur(5, 5);
                canvas.DrawBitmap(battleRoyaleMapCoverCurrent, new SKRect(0, 0, 1920, 600), paint);
                canvas.DrawBitmap(battleRoyaleRankedMapCoverCurrent, new SKRect(0, 600, 1920, 1200), paint);
                canvas.DrawBitmap(arenaMapCoverCurrent, new SKRect(0, 1200, 1920, 1800), paint);
                canvas.DrawBitmap(arenaRankedMapCoverCurrent, new SKRect(0, 1800, 1920, 2400), paint);
            }
            using (SKPaint textPaint = new())
            {
                textPaint.Typeface = font;
                textPaint.Color = SKColors.White;
                textPaint.TextAlign = SKTextAlign.Left;
                SKRect textBounds = new SKRect();

                #region 匹配
                textPaint.TextSize = 96;
                textPaint.MeasureText("大逃杀 匹配", ref textBounds);
                CanvasExtensions.DrawShapedText(canvas, "大逃杀 匹配", 40, 20 + textBounds.Height, textPaint);

                textPaint.TextSize = 192;
                var currentName = GetMapNameCN(rotation.battle_royale.current.map);
                textPaint.MeasureText(currentName, ref textBounds);
                CanvasExtensions.DrawShapedText(canvas, currentName, 40, 140 + textBounds.Height, textPaint);

                textPaint.TextSize = 40;
                var nextRotation = $"下一轮换：{GetMapNameCN(rotation.battle_royale.next.map)}";
                textPaint.MeasureText(nextRotation, ref textBounds);
                CanvasExtensions.DrawShapedText(canvas, nextRotation, 40, 450 + textBounds.Height, textPaint);

                var rotationDate = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1970, 1, 1), TimeZoneInfo.Local).AddTicks(rotation.battle_royale.current.end * 10000000);
                var countDown = rotationDate - DateTime.Now;
                if (countDown.TotalHours <= 24)
                {
                    var nextTime = $"剩余时间：{countDown.Hours:00}:{countDown.Minutes:00}:{countDown.Seconds:00}";
                    textPaint.MeasureText(nextTime, ref textBounds);
                    CanvasExtensions.DrawShapedText(canvas, nextTime, 40, 520 + textBounds.Height, textPaint);
                }
                #endregion

                #region 排位
                textPaint.TextSize = 96;
                textPaint.MeasureText("大逃杀 排位", ref textBounds);
                CanvasExtensions.DrawShapedText(canvas, "大逃杀 排位", 40, 600 + 20 + textBounds.Height, textPaint);

                textPaint.TextSize = 192;
                currentName = GetMapNameCN(rotation.ranked.current.map);
                textPaint.MeasureText(currentName, ref textBounds);
                CanvasExtensions.DrawShapedText(canvas, currentName, 40, 600 + 140 + textBounds.Height, textPaint);

                textPaint.TextSize = 40;
                nextRotation = $"下一轮换：{GetMapNameCN(rotation.ranked.next.map)}";
                textPaint.MeasureText(nextRotation, ref textBounds);
                CanvasExtensions.DrawShapedText(canvas, nextRotation, 40, 600 + 450 + textBounds.Height, textPaint);

                rotationDate = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1970, 1, 1), TimeZoneInfo.Local).AddTicks(rotation.ranked.current.end * 10000000);
                countDown = rotationDate - DateTime.Now;
                if (countDown.TotalHours <= 24)
                {
                    var nextTime = $"剩余时间：{countDown.Hours:00}:{countDown.Minutes:00}:{countDown.Seconds:00}";
                    textPaint.MeasureText(nextTime, ref textBounds);
                    CanvasExtensions.DrawShapedText(canvas, nextTime, 40, 600 + 520 + textBounds.Height, textPaint);
                }
                #endregion

                #region 竞技场
                textPaint.TextSize = 96;
                textPaint.MeasureText("竞技场 匹配", ref textBounds);
                CanvasExtensions.DrawShapedText(canvas, "竞技场 匹配", 40, 1200 + 20 + textBounds.Height, textPaint);

                textPaint.TextSize = 192;
                currentName = GetMapNameCN(rotation.arenas.current.map);
                textPaint.MeasureText(currentName, ref textBounds);
                CanvasExtensions.DrawShapedText(canvas, currentName, 40, 1200 + 140 + textBounds.Height, textPaint);

                textPaint.TextSize = 40;
                nextRotation = $"下一轮换：{GetMapNameCN(rotation.arenas.next.map)}";
                textPaint.MeasureText(nextRotation, ref textBounds);
                CanvasExtensions.DrawShapedText(canvas, nextRotation, 40, 1200 + 450 + textBounds.Height, textPaint);

                rotationDate = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1970, 1, 1), TimeZoneInfo.Local).AddTicks(rotation.arenas.current.end * 10000000);
                countDown = rotationDate - DateTime.Now;
                if (countDown.TotalHours <= 24)
                {
                    var nextTime = $"剩余时间：{countDown.Hours:00}:{countDown.Minutes:00}:{countDown.Seconds:00}";
                    textPaint.MeasureText(nextTime, ref textBounds);
                    CanvasExtensions.DrawShapedText(canvas, nextTime, 40, 1200 + 520 + textBounds.Height, textPaint);
                }
                #endregion

                #region 竞技场排位
                textPaint.TextSize = 96;
                textPaint.MeasureText("竞技场 排位", ref textBounds);
                CanvasExtensions.DrawShapedText(canvas, "竞技场 排位", 40, 1800 + 20 + textBounds.Height, textPaint);

                textPaint.TextSize = 192;
                currentName = GetMapNameCN(rotation.arenasRanked.current.map);
                textPaint.MeasureText(currentName, ref textBounds);
                CanvasExtensions.DrawShapedText(canvas, currentName, 40, 1800 + 140 + textBounds.Height, textPaint);

                textPaint.TextSize = 40;
                nextRotation = $"下一轮换：{GetMapNameCN(rotation.arenasRanked.next.map)}";
                textPaint.MeasureText(nextRotation, ref textBounds);
                CanvasExtensions.DrawShapedText(canvas, nextRotation, 40, 1800 + 450 + textBounds.Height, textPaint);

                rotationDate = TimeZoneInfo.ConvertTimeFromUtc(new DateTime(1970, 1, 1), TimeZoneInfo.Local).AddTicks(rotation.arenasRanked.current.end * 10000000);
                countDown = rotationDate - DateTime.Now;
                if (countDown.TotalHours <= 24)
                {
                    var nextTime = $"剩余时间：{countDown.Hours:00}:{countDown.Minutes:00}:{countDown.Seconds:00}";
                    textPaint.MeasureText(nextTime, ref textBounds);
                    CanvasExtensions.DrawShapedText(canvas, nextTime, 40, 1800 + 520 + textBounds.Height, textPaint);
                }
                #endregion

            }
            return surface.Snapshot().Encode(SKEncodedImageFormat.Jpeg, 80).ToArray();
        }

        private class ApexConfig
        {
            [JsonProperty("token")]
            public readonly string Token;
        }
    }

    public class PredatorInfo
    {
        public uint RankPoint;
        public uint ArenaPoint;
    }

    public class UidInfo
    {
        public string Error;
        public string name;
        public string uid;
    }

    public class PlayerStats
    {
        public Global global;
        public Realtime realtime;
        public Legend legends;
        public string? Error;


        public class Global
        {
            public string name;
            public uint level;
            public Ban bans;
            public Rank rank;
            public Rank arena;


            public class Ban
            {
                public bool isActive;
            }

            public class Rank
            {
                public int rankScore;
                public string rankName;
                public int rankDiv;
            }
        }

        public class Realtime
        {
            public int isOnline;
            public int isInGame;
        }

        public class Legend
        {
            public Selected selected;
            public Dictionary<string, LegendInfo> all;

            public class Selected
            {
                public string LegendName;
            }

            public class LegendInfo
            {
                public List<Data> data;
                public class Data
                {
                    public string name;
                    public uint value;
                }
            }
        }
    }

    class ApexMapRotation
    {
        public class Mode
        {
            public Info current;
            public Info next;

            public class Info
            {
                public long start;
                public long end;
                public string map;
                public string code;
                public string asset;
            }
        }

        public Mode battle_royale;
        public Mode arenas;
        public Mode ranked;
        public Mode arenasRanked;
    }

}
