using Newtonsoft.Json;
using NLog;
using RestSharp;
using RinBot.Command.Arcaea.Database;
using static RinBot.Command.Arcaea.AUAResult;

namespace RinBot.Command.Arcaea
{
    internal class ArcaeaUnlimitedAPI
    {
        #region Singleton
        public static ArcaeaUnlimitedAPI Instance = new Lazy<ArcaeaUnlimitedAPI>(() => new ArcaeaUnlimitedAPI()).Value;
        private ArcaeaUnlimitedAPI()
        {
            if (File.Exists(AUTH_PATH))
            {
                auth = JsonConvert.DeserializeObject<Auth>(File.ReadAllText(AUTH_PATH)) ?? new();
                RestClient = new(auth.Url);
                RestClient.Options.MaxTimeout = 60 * 1000;
            }
            else
            {
                File.WriteAllText(AUTH_PATH, JsonConvert.SerializeObject(new Auth()));
                throw new ArgumentNullException();
            }

            statusTable = new();
            if (File.Exists(STATUS_PATH))
            {
                var list = JsonConvert.DeserializeObject<List<AUAStatus>>(File.ReadAllText(STATUS_PATH)) ?? new();
                foreach (var status in list)
                {
                    statusTable.Add(status.Status, status);
                }
            }
        }
        #endregion

        public static string AUTH_PATH => Path.Combine(ArcaeaModule.RESOURCE_DIR_PATH, "auth.json");
        public static string STATUS_PATH => Path.Combine(ArcaeaModule.RESOURCE_DIR_PATH, "aua_status.json");
        private readonly Auth auth;
        private Dictionary<int, AUAStatus> statusTable;
        private static RestClient RestClient;
        private Logger Logger = LogManager.GetLogger("AUA");


        public async Task<B30Result?> GetBest30Result(string userCode)
        {
            var request = new RestRequest($"/user/best30?usercode={userCode}&overflow=3&withsonginfo=false");
            request.AddHeader("Authorization", $"Bearer {auth.BearerToken}");
            Logger.Info($"Querying Best30 Result: {userCode}");
            var response = await RestClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Logger.Info($"Querying Best30 Result Successful: {userCode}");
                return JsonConvert.DeserializeObject<B30Result>(response.Content!);
            }
            else
            {
                Logger.Error($"Querying Best30 Result Failed: {userCode}");
                return null;
            }
        }
        public async Task<BestPlayResult?> GetBestResult(string userCode, string songId, RatingClass ratingClass = RatingClass.Future)
        {
            var request = new RestRequest($"/user/best?usercode={userCode}&songid={songId}&difficulty={(int)ratingClass}&withsonginfo=false");
            request.AddHeader("Authorization", $"Bearer {auth.BearerToken}");
            Logger.Info($"Querying Best Result: {userCode} {songId} {ratingClass}");
            var response = await RestClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Logger.Info($"Querying Best Result Successful: {userCode} {songId} {ratingClass}");
                return JsonConvert.DeserializeObject<BestPlayResult>(response.Content!);
            }
            else
            {
                Logger.Error($"Querying Best Result Failed: {userCode} {songId} {ratingClass}");
                return null;
            }
        }
        public async Task<PlayerInfoResult?> GetPlayerInfoByCode(string userCode)
        {
            var request = new RestRequest($"/user/info?usercode={userCode}&withsonginfo=false");
            request.AddHeader("Authorization", $"Bearer {auth.BearerToken}");
            Logger.Info($"Querying PlayerInfo: {userCode}");
            var response = await RestClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Logger.Info($"Querying PlayerInfo Successful: {userCode}");
                return JsonConvert.DeserializeObject<PlayerInfoResult>(response.Content!);
            }
            else
            {
                Logger.Error($"Querying PlayerInfo Failed: {userCode}");
                return null;
            }
        }
        public async Task<PlayerInfoResult?> GetPlayerInfoByName(string userName)
        {
            var request = new RestRequest($"/user/info?user={userName}&withsonginfo=false");
            request.AddHeader("Authorization", $"Bearer {auth.BearerToken}");
            Logger.Info($"Querying PlayerInfo: {userName}");
            var response = await RestClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Logger.Info($"Querying PlayerInfo Successful: {userName}");
                return JsonConvert.DeserializeObject<PlayerInfoResult>(response.Content!);
            }
            else
            {
                Logger.Error($"Querying PlayerInfo Failed: {userName}");
                return null;
            }
        }
        public async Task<SongInfoResult?> GetSongInfo(string songId)
        {
            var request = new RestRequest($"/song/info?songid={songId}");
            request.AddHeader("Authorization", $"Bearer {auth.BearerToken}");
            Logger.Info($"Get SongInfo: {songId}");
            var response = await RestClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Logger.Info($"Get SongInfo Successful: {songId}");
                return JsonConvert.DeserializeObject<SongInfoResult>(response.Content!);
            }
            else
            {
                Logger.Error($"Get SongInfo Failed: {songId}");
                return null;
            }
        }
        public async Task<byte[]> GetSongCover(string songId, RatingClass ratingClass = RatingClass.Future)
        {
            var request = new RestRequest($"/assets/song?songid={songId}&difficulty={((int)ratingClass)}");
            request.AddHeader("Authorization", $"Bearer {auth.BearerToken}");
            Logger.Info($"Download Song Cover: {songId} {ratingClass}");
            var response = await RestClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Logger.Info($"Download Song Cover Successful: {songId} {ratingClass}");
                return response.RawBytes!;
            }
            else
            {
                Logger.Error($"Download Song Cover Failed: {songId} {ratingClass}");
                return Array.Empty<byte>();
            }
        }
        public async Task<byte[]> GetChartPreview(string songId, RatingClass ratingClass = RatingClass.Future)
        {
            var request = new RestRequest($"/assets/preview?songid={songId}&difficulty={((int)ratingClass)}");
            request.AddHeader("Authorization", $"Bearer {auth.BearerToken}");
            Logger.Info($"Download Chart Preview: {songId} {ratingClass}");
            var response = await RestClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Logger.Info($"Download Chart Preview Successful: {songId} {ratingClass}");
                return response.RawBytes!;
            }
            else
            {
                Logger.Error($"Download Chart Preview Failed: {songId} {ratingClass}");
                return Array.Empty<byte>();
            }
        }
        public async Task<byte[]> GetCharaIllust(uint chara, bool isUncapped = false)
        {
            var request = new RestRequest($"/assets/char?partner={chara}&awakened={isUncapped}");
            request.AddHeader("Authorization", $"Bearer {auth.BearerToken}");
            Logger.Info($"Download Character Illust: {chara} {(isUncapped ? "uncapped" : "")}");
            var response = await RestClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Logger.Info($"Download Character Illust Successful: {chara} {(isUncapped ? "uncapped" : "")}");
                return response.RawBytes!;
            }
            else
            {
                Logger.Warn($"Download Character Illust Failed: {chara} {(isUncapped ? "uncapped" : "")}");
                return Array.Empty<byte>();
            }
        }
        public async Task<byte[]> GetCharaIcon(uint chara, bool isUncapped = false)
        {
            var request = new RestRequest($"/assets/icon?partner={chara}&awakened={isUncapped}");
            request.AddHeader("Authorization", $"Bearer {auth.BearerToken}");
            Logger.Info($"Download Character Icon: {chara} {(isUncapped ? "uncapped" : "")}");
            var response = await RestClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Logger.Info($"Download Character Icon Successful: {chara} {(isUncapped ? "uncapped" : "")}");
                return response.RawBytes!;
            }
            else
            {
                Logger.Warn($"Download Character Icon Failed: {chara} {(isUncapped ? "uncapped" : "")}");
                return Array.Empty<byte>();
            }
        }

        public AUAStatus? GetStatus(int statusCode)
            => statusTable.TryGetValue(statusCode, out var status) ? status : null;
    }



    internal class Auth
    {
        [JsonProperty("url")]
        public string Url { get; set; } = "url";
        [JsonProperty("bearer_token")]
        public string BearerToken { get; set; } = "token";
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
