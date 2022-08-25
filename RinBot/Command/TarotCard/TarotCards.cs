using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RinBot.Command.TarotCard
{
    internal static class TarotCards
    {
        private static readonly string TAROT_IMAGE_DIR_PATH = Path.Combine(TarotCardModule.RESOURCE_DIR_PATH, "Image");
        private static readonly string TAROT_DESCRIPTION_PATH = Path.Combine(TarotCardModule.RESOURCE_DIR_PATH, "descriptions.json");
        private static Dictionary<int, TarotCardInfo> tarotCardTable;
        static TarotCards()
        {
            if (File.Exists(TAROT_DESCRIPTION_PATH))
            {
                var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, TarotCardInfo>>(File.ReadAllText(TAROT_DESCRIPTION_PATH));
                var images = new DirectoryInfo(TAROT_IMAGE_DIR_PATH).EnumerateFiles().ToArray();
                tarotCardTable = new();
                foreach (var keyPair in jsonDict)
                {
                    keyPair.Value.ImagePath = images.First(x => x.Name.StartsWith(keyPair.Key.PadLeft(2, '0'))).FullName;
                    tarotCardTable.Add(int.Parse(keyPair.Key), keyPair.Value);
                }
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        public static List<TarotCardInfo> GetTarotCards(int num = 1, Random random = null)
        {
            random ??= new();
            List<TarotCardInfo> tarots = new();
            foreach (var info in tarotCardTable.Values)
            {
                tarots.Add(new TarotCardInfo()
                {
                    Name = info.Name,
                    NameEN = info.NameEN,
                    ImagePath = info.ImagePath,
                    Info = info.Info,
                });
            }
            tarots = tarots.OrderBy(x => random.Next()).Take(num).ToList();
            foreach (var info in tarots)
                info.IsReversed = random.Next(2) == 1;
            return tarots;
        }
    }
}
