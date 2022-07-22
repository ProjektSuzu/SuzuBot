using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RinBot.Core;

namespace RinBot.Command.Apex
{
    internal class ApexAPI
    {
        #region Singleton
        private static ApexAPI instance;
        public static ApexAPI Instance
        {
            get
            {
                if (instance == null) instance = new();
                return instance;
            }
        }
        private ApexAPI()
        {
            if (!File.Exists(CONFIG_PATH))
            {
                Logger.Fatal("配置文件不存在");
                throw new FileNotFoundException(CONFIG_PATH);
            }
            config = JsonConvert.DeserializeObject<ApexConfig>(File.ReadAllText(CONFIG_PATH));
            if (config == null)
            {
                Logger.Fatal("配置文件损坏");
                throw new FileLoadException(CONFIG_PATH);
            }
            Logger.Info("配置文件读取成功");
        }
        #endregion

        private Logger Logger = LogManager.GetLogger("APEX");
        private static readonly string APEX_RESOURCE_PATH = Path.Combine(Global.RESOURCE_PATH, "Apex");
        private static readonly string CONFIG_PATH = Path.Combine(APEX_RESOURCE_PATH, "apex_token.json");
        private const string STATUS_NAME_API =
           @"https://api.mozambiquehe.re/bridge?version=5&platform=PC&player={player}&auth={token}";
        private const string STATUS_UID_API =
           @"https://api.mozambiquehe.re/bridge?version=5&platform=PC&uid={player}&auth={token}";
        private const string PREDATOR_API =
            @"https://api.mozambiquehe.re/predator?auth={token}";
        private const string MAP_ROTATION_API =
            @"https://api.mozambiquehe.re/maprotation?auth={token}&version=2";

        private ApexConfig config;
        private HttpClient httpClient = new()
        {
            Timeout = new TimeSpan(0, 1, 0)
        };

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

        public async Task<PlayerStats?> GetPlayerStatsByName(string playerName)
        {
            var url = STATUS_NAME_API.Replace("{player}", playerName).Replace("{token}", config.Token);
            var response = httpClient.GetAsync(url).Result;
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PlayerStats>(responseString);
        }

        public async Task<PlayerStats?> GetPlayerStatsByUID(string playerId)
        {
            var url = STATUS_UID_API.Replace("{player}", playerId).Replace("{token}", config.Token);
            var response = httpClient.GetAsync(url).Result;
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<PlayerStats>(responseString);
        }

        public async Task<PredatorInfo?> GetPredatorInfo()
        {
            var url = PREDATOR_API.Replace("{token}", config.Token);
            var response = httpClient.GetAsync(url).Result;
            var responseString = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<JObject>(responseString);
            if (json == null) return null;
            var rp = json["RP"]["PC"]["val"].Value<uint>();
            var ap = json["AP"]["PC"]["val"].Value<uint>();

            return new()
            {
                RankPoint = rp,
                ArenaPoint = ap
            };
        }


    }

    class ApexConfig
    {
        [JsonProperty("token")]
        public readonly string Token;
    }

    public class PredatorInfo
    {
        public uint RankPoint;
        public uint ArenaPoint;
    }

    public class PlayerStats
    {
        public ApexGlobal global;
        public Realtime realtime;
        public Legend legends;
        public string? Error;


        public class ApexGlobal
        {
            public string name;
            public string uid;
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
