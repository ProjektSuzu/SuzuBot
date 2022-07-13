using Newtonsoft.Json;
using NLog;
using RinBot.Core;
using System.Net.Http.Headers;
using static RinBot.Command.Arcaea.SongResult;

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

            if (!File.Exists(STATUS_PATH))
            {
                Logger.Fatal("配置文件不存在");
                throw new FileNotFoundException(STATUS_PATH);
            }
            statusList = JsonConvert.DeserializeObject<List<AUAStatus>>(File.ReadAllText(STATUS_PATH));
            if (config == null)
            {
                Logger.Fatal("配置文件损坏");
                throw new FileLoadException(STATUS_PATH);
            }
            Logger.Info("配置文件读取成功");
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("User-Agent", config.Token));
        }
        #endregion
        private Logger Logger = LogManager.GetLogger("AUA");
        private static readonly string ARCAEA_RESOURCE_PATH = Path.Combine(Global.RESOURCE_PATH, "Arcaea");
        private static readonly string CONFIG_PATH = Path.Combine(ARCAEA_RESOURCE_PATH, "aua_token.json");
        private static readonly string STATUS_PATH = Path.Combine(ARCAEA_RESOURCE_PATH, "aua_status.json");

        private List<AUAStatus> statusList;
        private ArcaeaUserDB ArcaeaUserDB = ArcaeaUserDB.Instance;
        private AUAConfig config;
        private HttpClient httpClient = new()
        {
            Timeout = new TimeSpan(0, 1, 0)
        };
        public async Task<B30Result?> GetBest30Result(string userCode)
        {
            Logger.Info($"Querying Best30Result: {userCode}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/user/best30?usercode={userCode}&overflow=3&withsonginfo=false").Result;
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Http connection failed: {response.StatusCode}");
                    return null;
                }
                Logger.Info($"Query Best30Result success: {userCode}");
                var result = JsonConvert.DeserializeObject<B30Result>(response.Content.ReadAsStringAsync().Result);
                return result;
            }
            catch (TaskCanceledException taskCanceled)
            {
                Logger.Error($"Timeout when Querying BeBest30ResultstInfo: {userCode}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexcept Error Occured: {e.Message}");
                return null;
            }
        }
        public async Task<BestPlayResult?> GetBestResult(string userCode, string songId, SongDifficulty difficulty = SongDifficulty.Future)
        {
            Logger.Info($"Querying BestResult: {userCode} {songId} {difficulty}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/user/best?usercode={userCode}&songid={songId}&difficulty={(int)difficulty}&withsonginfo=false").Result;
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Http connection failed: {response.StatusCode}");
                    return null;
                }
                Logger.Info($"Query BestResult success: {userCode} {songId} {difficulty}");
                var result = JsonConvert.DeserializeObject<BestPlayResult>(response.Content.ReadAsStringAsync().Result);
                return result;
            }
            catch (TaskCanceledException taskCanceled)
            {
                Logger.Error($"Timeout when Querying BestResult: {userCode} {songId} {difficulty}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexcept Error Occured: {e.Message}");
                return null;
            }
        }
        public async Task<PlayerInfoResult?> GetPlayerInfo(string userCode)
        {
            Logger.Info($"Querying PlayerInfo via UserCode: {userCode}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/user/info?usercode={userCode}&withsonginfo=false").Result;
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Http connection failed: {response.StatusCode}");
                    return null;
                }
                Logger.Info($"Query PlayerInfo Success via UserCode: {userCode}");
                var result = JsonConvert.DeserializeObject<PlayerInfoResult>(response.Content.ReadAsStringAsync().Result);
                if (result != null && result.Status == 0)
                    ArcaeaUserDB.UpdatePlayerInfo(result.Content.AccountInfo.UserCode, result.Content.AccountInfo.UserName);
                return result;
            }
            catch (TaskCanceledException taskCanceled)
            {
                Logger.Error($"Timeout when Querying PlayerInfo via UserCode: {userCode}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexcept Error Occured: {e.Message}");
                return null;
            }
        }
        public async Task<PlayerInfoResult?> GetPlayerInfoByName(string userName)
        {
            Logger.Info($"Querying PlayerInfo via UserId: {userName}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/user/info?user={userName}&withsonginfo=false").Result;
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Http connection failed: {response.StatusCode}");
                    return null;
                }
                Logger.Info($"Query PlayerInfo Success via UserId: {userName}");
                var result = JsonConvert.DeserializeObject<PlayerInfoResult>(response.Content.ReadAsStringAsync().Result);
                if (result != null && result.Status == 0)
                    ArcaeaUserDB.UpdatePlayerInfo(result.Content.AccountInfo.UserCode, result.Content.AccountInfo.UserName);
                return result;
            }
            catch (TaskCanceledException taskCanceled)
            {
                Logger.Error($"Timeout when Querying PlayerInfo via UserId: {userName}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexcept Error Occured: {e.Message}");
                return null;
            }
        }


        public async Task<SongInfoResult?> GetSongInfo(string songId)
        {
            Logger.Info($"Querying SongInfo: {songId}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/song/info?songid={songId}&withsonginfo=false").Result;
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Http connection failed: {response.StatusCode}");
                    return null;
                }
                Logger.Info($"Query SongInfo success: {songId}");
                var result = JsonConvert.DeserializeObject<SongInfoResult>(response.Content.ReadAsStringAsync().Result);
                if (result != null && result.Status == 0)
                {
                    result.Content.Difficulties.ForEach(x => x.SongId = result.Content.SongId);
                }
                return result;
            }
            catch (TaskCanceledException taskCanceled)
            {
                Logger.Error($"Timeout when Querying SongInfo: {songId}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexcept Error Occured: {e.Message}");
                return null;
            }
        }
        public async Task<byte[]?> GetChartPreview(string songId, SongDifficulty difficulty)
        {
            Logger.Info($"Downloading chart preview: {songId} {difficulty}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/assets/preview?songid={songId}&difficulty={difficulty}").Result;
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Http connection failed: {response.StatusCode}");
                    return null;
                }
                else
                {
                    Logger.Info($"Download chart preview success: {songId} {difficulty}");
                    return response.Content.ReadAsByteArrayAsync().Result;
                }
            }
            catch (TaskCanceledException taskCanceled)
            {
                Logger.Error($"Timeout when downloading chart preview: {songId} {difficulty}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexcept Error Occured: {e.Message}");
                return null;
            }
        }
        public async Task<byte[]?> GetSongCover(string sid, bool beyond = false)
        {
            Logger.Info($"Downloading song cover: {sid} {(beyond ? "BYD" : "")}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/assets/song?songid={sid}{(beyond ? "&difficulty=byn" : "")}").Result;
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Http connection failed: {response.StatusCode}");
                    return null;
                }
                else
                {
                    Logger.Info($"Download song cover success: {sid} {(beyond ? "BYD" : "")}");
                    return response.Content.ReadAsByteArrayAsync().Result;
                }
            }
            catch (TaskCanceledException taskCanceled)
            {
                Logger.Error($"Timeout when downloading song cover: {sid} {(beyond ? "BYD" : "")}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexcept Error Occured: {e.Message}");
                return null;
            }
        }
        public async Task<byte[]?> GetCharacterImage(uint chara, bool isUncapped = false)
        {
            Logger.Info($"Downloading character cover: {chara} {(isUncapped ? "uncapped" : "")}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/assets/char?partner={chara}&awakened={isUncapped}").Result;
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Http connection failed: {response.StatusCode}");
                    return null;
                }
                else
                {
                    Logger.Info($"Download character cover success: {chara} {(isUncapped ? "uncapped" : "")}");
                    return response.Content.ReadAsByteArrayAsync().Result;
                }
            }
            catch (TaskCanceledException taskCanceled)
            {
                Logger.Error($"Timeout when downloading character cover: {chara} {(isUncapped ? "uncapped" : "")}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexcept Error Occured: {e.Message}");
                return null;
            }
        }
        public async Task<byte[]?> GetCharacterIcon(uint chara, bool isUncapped = false)
        {
            Logger.Info($"Downloading character icon: {chara} {(isUncapped ? "uncapped" : "")}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/assets/icon?partner={chara}&awakened={isUncapped}").Result;
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Http connection failed: {response.StatusCode}");
                    return null;
                }
                else
                {
                    Logger.Info($"Download character icon success: {chara} {(isUncapped ? "uncapped" : "")}");
                    return response.Content.ReadAsByteArrayAsync().Result;
                }
            }
            catch (TaskCanceledException taskCanceled)
            {
                Logger.Error($"Timeout when downloading character icon: {chara} {(isUncapped ? "uncapped" : "")}");
                return null;
            }
            catch (Exception e)
            {
                Logger.Error($"Unexcept Error Occured: {e.Message}");
                return null;
            }
        }
        public string GetStatusTranslation(int statusCode)
        {
            return statusList.FirstOrDefault(x => x.Status == statusCode)?.Translation ?? "unknow";
        }
    }

    internal class AUAConfig
    {
        [JsonProperty("api")]
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
