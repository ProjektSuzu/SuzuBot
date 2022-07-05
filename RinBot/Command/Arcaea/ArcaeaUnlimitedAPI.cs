using Newtonsoft.Json;
using NLog;
using RinBot.Core;
using System.Net.Http.Headers;

namespace RinBot.Command.Arcaea
{
    internal class ArcaeaUnlimitedAPI
    {
        #region Singleton
        private static ArcaeaUnlimitedAPI instance;
        public static ArcaeaUnlimitedAPI Instance
        {
            get
            {
                if (instance == null) instance = new();
                return instance;
            }
        }
        private ArcaeaUnlimitedAPI()
        {
            if (!File.Exists(CONFIG_PATH))
            {
                Logger.Fatal("配置文件不存在");
                throw new FileNotFoundException(CONFIG_PATH);
            }
            config = JsonConvert.DeserializeObject<AUAConfig>(File.ReadAllText(CONFIG_PATH));
            if (config == null)
            {
                Logger.Fatal("配置文件损坏");
                throw new FileLoadException(CONFIG_PATH);
            }
            Logger.Info("配置文件读取成功");
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("User-Agent", config.Token));
        }
        #endregion
        private Logger Logger = LogManager.GetLogger("AUA");
        private static readonly string ARCAEA_RESOURCE_PATH = Path.Combine(Global.RESOURCE_PATH, "Arcaea");
        private static readonly string CONFIG_PATH = Path.Combine(ARCAEA_RESOURCE_PATH, "aua_token.json");
        private static readonly string STATUS_PATH = Path.Combine(ARCAEA_RESOURCE_PATH, "aua_status.json");

        private ArcaeaUserDB ArcaeaUserDB = ArcaeaUserDB.Instance;
        private AUAConfig config;
        private HttpClient httpClient = new()
        {
            Timeout = new TimeSpan(0, 0, 30)
        };

        public async Task<PlayerInfoResult?> GetPlayerInfo(string userCode)
        {
            Logger.Info($"Querying PlayerInfo: {userCode}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/user/info?usercode={userCode}&withsonginfo=false").Result;
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Http connection failed: {response.StatusCode}");
                    return null;
                }
                Logger.Info($"Query PlayerInfo Success: {userCode}");
                var result = JsonConvert.DeserializeObject<PlayerInfoResult>(response.Content.ReadAsStringAsync().Result);
                if (result != null)
                    ArcaeaUserDB.UpdatePlayerInfo(result.Content.AccountInfo.UserCode, result.Content.AccountInfo.UserName);
                return result;
            }
            catch (TaskCanceledException taskCanceled)
            {
                Logger.Error($"Timeout when Querying PlayerInfo: {userCode}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexcept Error Occured: {e.Message}");
                return null;
            }
        }
    }

    internal class AUAConfig
    {
        [JsonProperty("api_url")]
        public string API { get; set; }
        [JsonProperty("token")]
        public string Token { get; set; }
    }

    internal class AUAStatus
    {
        [JsonProperty("status")]
        public int Status { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("translation")]
        public string Translation { get; set; }
    }
}
