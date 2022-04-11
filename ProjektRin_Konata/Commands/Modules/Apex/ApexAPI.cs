using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
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

        private static readonly string api =
            @"https://public-api.tracker.gg/v2/apex/standard/profile/{platform}/{platformUserIdentifier}";

        private static readonly string TAG = "APEX";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        public async Task<StatsProfile?> GetPlayerStats(string userId, string platform = "origin")
        {
            HttpClient httpClient = new HttpClient()
            {
                Timeout = new TimeSpan(0, 0, 15)
            };

            httpClient.DefaultRequestHeaders.Add("TRN-Api-Key", config.Token);
            var url = api.Replace("{platform}", platform).Replace("{platformUserIdentifier}", userId);
            var response = await httpClient.GetAsync(url);
            Logger.Info($"Response: {response.StatusCode}");
            return JsonConvert.DeserializeObject<StatsProfile>(response.Content.ReadAsStringAsync().Result);
        }

        private class ApexConfig
        {
            [JsonProperty("token")]
            public readonly string Token;
        }
    }

    public class StatsProfile
    {
        public Data data;
        public List<Error> errors;
        public class Data
        {
            public PlatformInfo platformInfo;
            public List<Segment> segments;
            public class PlatformInfo
            {
                public string platformSlug;
                public string platformUserIdentifier;
            }

            public class Segment
            {
                public string type;
                public Metadata metadata;
                public Dictionary<string, Stat> stats;

                public class Metadata
                {
                    public string name;
                    public bool isActive;
                }

                public class Stat
                {
                    public string displayName;
                    public Metadata metadata;
                    public string displayValue;

                    public class Metadata
                    {
                        public string rankName;
                    }
                }
            }
        }

        public class Error
        {
            public string code;
            public string message;
        }
    }

    
}
