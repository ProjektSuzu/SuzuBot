using Newtonsoft.Json;
using RinBot.Command.Arcaea;
using RinBot.Core;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Command.Tarot
{
    internal class TarotManager
    {
        private static readonly string TAROT_PATH = Path.Combine(Global.RESOURCE_PATH, "tarot.json");
        private static readonly string IMG_PATH = Path.Combine(Global.RESOURCE_PATH, "Tarot");
        #region Singleton
        private static TarotManager instance;
        public static TarotManager Instance
        {
            get
            {
                if (instance == null) instance = new();
                return instance;
            }
        }
        private TarotManager()
        {
            tarots = JsonConvert.DeserializeObject<List<TarotInfo>>(File.ReadAllText(TAROT_PATH)) ?? new();
        }
        #endregion

        private List<TarotInfo> tarots;

        public List<TarotResult> GetTarotResults(int num = 1, Random random = null)
        {
            if (random == null) random = new();
            return tarots.OrderBy(x => random.Next()).Take(num).Select(x => new TarotResult { Title = x.Title, IsReversed = random.Next(2) == 1 }).ToList();
        }

        public string GetTarotMeaning(string title, bool isReversed)
        {
            var tarot = tarots.First(x => x.Title == title);
            return isReversed ? tarot.Negative : tarot.Positive;
        }

        public byte[] GetTarotImage(string title)
        {
            return File.ReadAllBytes(Path.Combine(IMG_PATH, $"{title}.png"));
        }
    }

    public class TarotResult
    {
        public string Title { get; set; }
        public bool IsReversed { get; set; }
    }

    public class TarotInfo
    {
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("positive")]
        public string Positive { get; set; }
        [JsonProperty("negative")]
        public string Negative { get; set; }
    }
}
