using Newtonsoft.Json;
using NLog;
using RinBot.Core.Components;
using System.Globalization;
using System.Net.Http.Headers;
using static RinBot.Commands.Modules.Arcaea.SongResult;

namespace RinBot.Commands.Modules.Arcaea
{
    internal class ArcaeaUnlimitedAPI
    {
        private static readonly string configPath = Path.Combine(BotManager.resourcePath, "ArcaeaProbe_Skia/config.json");

        private HttpClient httpClient = new HttpClient()
        {
            Timeout = new TimeSpan(0, 0, 10)
        };

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
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("User-Agent", config.Token));
        }
        #endregion

        public async Task<B30Result?> GetB30(string usercode)
        {
            Logger.Info($"B30 Querying: {usercode}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/user/best30?usercode={usercode}&overflow=3&withsonginfo=false").Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    Logger.Info($"Query Success: {usercode}");
                    B30Result? result = JsonConvert.DeserializeObject<B30Result>(response.Content.ReadAsStringAsync().Result);
                    ArcaeaUserInfo? info = ArcUserInfo.Instance.GetByUserCode(usercode);
                    if (info != null && result != null)
                    {
                        info.B30Json = response.Content.ReadAsStringAsync().Result;
                        info.PTT = result.content.account_info.rating;
                        info.LastUpdate = DateTime.Now;
                        if (info.RecordJson != null && info.RecordJson != "")
                        {
                            var records = JsonConvert.DeserializeObject<List<PttRecord>>(info.RecordJson);
                            if (records != null && records.Count > 0)
                            {
                                var record = records.Last();
                                var date = DateTime.ParseExact(record.Date, "yyMMdd", CultureInfo.InvariantCulture);
                                if (DateTime.Now - date > TimeSpan.FromDays(1))
                                {
                                    var dateStr = DateTime.Now.ToString("yyMMdd");
                                    var ptt = result.content.account_info.rating;
                                    records.Add(new PttRecord() { Date = dateStr, PTT = ptt });
                                }
                                info.RecordJson = JsonConvert.SerializeObject(records);
                            }
                            else
                            {
                                var dateStr = DateTime.Now.ToString("yyMMdd");
                                var ptt = result.content.account_info.rating;
                                records = new()
                                {
                                    new PttRecord() { Date = dateStr, PTT = ptt }
                                };
                                info.RecordJson = JsonConvert.SerializeObject(records);
                            }
                        }
                        else
                        {
                            var dateStr = DateTime.Now.ToString("yyMMdd");
                            var ptt = result.content.account_info.rating;
                            var records = new List<PttRecord>()
                                {
                                    new PttRecord() { Date = dateStr, PTT = ptt }
                                };
                            info.RecordJson = JsonConvert.SerializeObject(records);
                        }
                        ArcUserInfo.Instance.UpdateUserInfo(info);
                    }

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
            Logger.Info($"Best Querying: {usercode}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/user/best?usercode={usercode}&songid={sid}&difficulty={(int)difficulty}&withsonginfo=false").Result;
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
            Logger.Info($"Info Querying: {usercode}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/user/info?usercode={usercode}&withsonginfo=false").Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    Logger.Info($"Query Success: {usercode}");
                    var result = JsonConvert.DeserializeObject<UserInfoResult>(response.Content.ReadAsStringAsync().Result);
                    ArcaeaUserInfo? info = ArcUserInfo.Instance.GetByUserCode(usercode);
                    if (info != null && result != null)
                    {
                        info.PTT = result.content.account_info.rating;
                        info.LastUpdate = DateTime.Now;
                        if (info.RecordJson != null && info.RecordJson != "")
                        {
                            var records = JsonConvert.DeserializeObject<List<PttRecord>>(info.RecordJson);
                            if (records != null && records.Count > 0)
                            {
                                var record = records.Last();
                                var date = DateTime.ParseExact(record.Date, "yyMMdd", CultureInfo.InvariantCulture);
                                if (DateTime.Now - date > TimeSpan.FromDays(1))
                                {
                                    var dateStr = DateTime.Now.ToString("yyMMdd");
                                    var ptt = result.content.account_info.rating;
                                    records.Add(new PttRecord() { Date = dateStr, PTT = ptt });
                                }
                                info.RecordJson = JsonConvert.SerializeObject(records);
                            }
                            else
                            {
                                var dateStr = DateTime.Now.ToString("yyMMdd");
                                var ptt = result.content.account_info.rating;
                                records = new()
                                {
                                    new PttRecord() { Date = dateStr, PTT = ptt }
                                };
                                info.RecordJson = JsonConvert.SerializeObject(records);
                            }
                        }
                        else
                        {
                            var dateStr = DateTime.Now.ToString("yyMMdd");
                            var ptt = result.content.account_info.rating;
                            var records = new List<PttRecord>()
                                {
                                    new PttRecord() { Date = dateStr, PTT = ptt }
                                };
                            info.RecordJson = JsonConvert.SerializeObject(records);
                        }
                        ArcUserInfo.Instance.UpdateUserInfo(info);
                    }
                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<UserInfoResult?> GetUserInfoByName(string user)
        {
            Logger.Info($"Info Querying: {user}");
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/user/info?user={user}&withsonginfo=false").Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    Logger.Info($"Query Success: {user}");
                    var result = JsonConvert.DeserializeObject<UserInfoResult>(response.Content.ReadAsStringAsync().Result);
                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]?> GetCharaImg(uint chara, bool isUncapped = false)
        {
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/assets/char?partner={chara}&awakened={isUncapped}").Result;
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
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/assets/icon?partner={chara}&awakened={isUncapped}").Result;
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
            try
            {
                HttpResponseMessage? response = httpClient.GetAsync($"{config.API}/assets/song?songid={sid}{(beyond ? "&difficulty=byn" : "")}").Result;
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
