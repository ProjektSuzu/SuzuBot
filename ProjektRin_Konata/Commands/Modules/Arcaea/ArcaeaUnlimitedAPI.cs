using Newtonsoft.Json;
using NLog;
using ProjektRin.Components;
using System.Net.Http.Headers;
using static ProjektRin.Commands.Modules.Arcaea.SongResult;

namespace ProjektRin.Commands.Modules.Arcaea
{
    internal class ArcaeaUnlimitedAPI
    {
        private static readonly string configPath = Path.Combine(BotManager.resourcePath, "ArcaeaProbe_Skia/config.json");

        private readonly AUAConfig config;

        private static readonly string TAG = "AUA";
        private static readonly Logger Logger = LogManager.GetLogger(TAG);

        #region 单例模式
        private static ArcaeaUnlimitedAPI instance;
        public static ArcaeaUnlimitedAPI Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ArcaeaUnlimitedAPI();
                }
                return instance;
            }
        }
        private ArcaeaUnlimitedAPI()
        {
            if (File.Exists(configPath))
            {
                config = JsonConvert.DeserializeObject<AUAConfig>(File.ReadAllText(configPath));
            }
        }
        #endregion

        public async Task<B30Result?> GetB30(string usercode)
        {
            HttpClient client = new();
            client.Timeout = new TimeSpan(0, 0, 10);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("User-Agent", config.Token));
            Logger.Info($"B30 Querying: {usercode}");
            try
            {
                HttpResponseMessage? response = client.GetAsync($"{config.API}/user/best30?usercode={usercode}&overflow=3&withsonginfo=false").Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    Logger.Info($"Query Success: {usercode}");
                    B30Result? result = JsonConvert.DeserializeObject<B30Result>(response.Content.ReadAsStringAsync().Result);
                    ArcaeaUserInfo? info = ArcUserInfoDB.Instance.GetByUserCode(usercode);
                    info.B30Json = response.Content.ReadAsStringAsync().Result;
                    info.LastUpdate = DateTime.Now;
                    ArcUserInfoDB.Instance.Update(info);
                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<BestPlayResult?> GetUserBest(string usercode, string sid, Difficulty difficulty)
        {
            HttpClient client = new();
            client.Timeout = new TimeSpan(0, 0, 30);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("User-Agent", config.Token));
            Logger.Info($"Best Querying: {usercode}");
            try
            {
                HttpResponseMessage? response = client.GetAsync($"{config.API}/user/best?usercode={usercode}&songid={sid}&difficulty={(int)difficulty}&withsonginfo=false").Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    Logger.Info($"Query Success: {usercode}");
                    return JsonConvert.DeserializeObject<BestPlayResult>(response.Content.ReadAsStringAsync().Result);
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<UserInfoResult?> GetUserInfo(string usercode)
        {
            HttpClient client = new();
            client.Timeout = new TimeSpan(0, 0, 30);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("User-Agent", config.Token));

            Logger.Info($"Info Querying: {usercode}");
            try
            {
                HttpResponseMessage? response = client.GetAsync($"{config.API}/user/info?usercode={usercode}&withsonginfo=false").Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    Logger.Info($"Query Success: {usercode}");
                    return JsonConvert.DeserializeObject<UserInfoResult>(response.Content.ReadAsStringAsync().Result);
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<UserInfoResult?> GetUserInfoByName(string user)
        {
            HttpClient client = new();
            client.Timeout = new TimeSpan(0, 0, 30);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("User-Agent", config.Token));

            Logger.Info($"Info Querying: {user}");
            try
            {
                HttpResponseMessage? response = client.GetAsync($"{config.API}/user/info?user={user}&withsonginfo=false").Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    Logger.Info($"Query Success: {user}");
                    return JsonConvert.DeserializeObject<UserInfoResult>(response.Content.ReadAsStringAsync().Result);
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]?> GetCharaImg(uint chara, bool isUncapped = false)
        {
            HttpClient client = new();
            client.Timeout = new TimeSpan(0, 0, 30);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("User-Agent", config.Token));
            try
            {
                HttpResponseMessage? response = client.GetAsync($"{config.API}/assets/char?partner={chara}&awakened={isUncapped}").Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    return response.Content.ReadAsByteArrayAsync().Result;
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]?> GetCharaIcon(uint chara, bool isUncapped = false)
        {
            HttpClient client = new();
            client.Timeout = new TimeSpan(0, 0, 30);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("User-Agent", config.Token));
            try
            {
                HttpResponseMessage? response = client.GetAsync($"{config.API}/assets/icon?partner={chara}&awakened={isUncapped}").Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    return response.Content.ReadAsByteArrayAsync().Result;
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]?> GetSongCover(string sid, bool beyond = false)
        {
            HttpClient client = new();
            client.Timeout = new TimeSpan(0, 0, 30);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("User-Agent", config.Token));
            try
            {
                HttpResponseMessage? response = client.GetAsync($"{config.API}/assets/song?songid={sid}{(beyond ? "&difficulty=byn" : "")}").Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    return response.Content.ReadAsByteArrayAsync().Result;
                }
            }
            catch
            {
                return null;
            }
        }


        private class AUAConfig
        {
            [JsonProperty("api")]
            public readonly string API;
            [JsonProperty("token")]
            public readonly string Token;
        }
    }
}
