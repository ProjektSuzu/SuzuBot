using Newtonsoft.Json;
using NLog;
using ProjektRin.Components;

namespace ProjektRin.Commands.Modules.Apex
{
    internal class ApexAPI
    {
        private static readonly string configPath = Path.Combine(BotManager.resourcePath, "ApexProbe/config.json");
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

        private static readonly string statsApi =
            @"https://api.mozambiquehe.re/bridge?version=5&platform=PC&player={player}&auth={token}";
        private static readonly string N2UApi =
            @"https://api.mozambiquehe.re/nametouid?player={player}&platform=PC&auth={token}";

        private static readonly string TAG = "APEX";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public async Task<PlayerStats?> GetPlayerStats(string userId)
        {
            HttpClient httpClient = new HttpClient()
            {
                Timeout = new TimeSpan(0, 0, 15)
            };
            var url = statsApi.Replace("{player}", userId).Replace("{token}", config.Token);
            var response = httpClient.GetAsync(url).Result;
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PlayerStats>(responseString);

        }

        public async Task<UidInfo> GetPlayerUid(string userName)
        {
            HttpClient httpClient = new HttpClient()
            {
                Timeout = new TimeSpan(0, 0, 15)
            };
            var url = N2UApi.Replace("{player}", userName).Replace("{token}", config.Token);
            var response = httpClient.GetAsync(url).Result;
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<UidInfo>(responseString);
        }

        private class ApexConfig
        {
            [JsonProperty("token")]
            public readonly string Token;
        }
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


}
