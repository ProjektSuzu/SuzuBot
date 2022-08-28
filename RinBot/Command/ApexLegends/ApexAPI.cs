using Newtonsoft.Json;
using NLog;
using RestSharp;
using static RinBot.Command.Arcaea.AUAResult;

namespace RinBot.Command.ApexLegends
{
    internal class ApexAPI
    {
        public ApexAPI()
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
        }
        public static string AUTH_PATH => Path.Combine(ApexModule.RESOURCE_DIR_PATH, "auth.json");
        private Auth auth;
        private static RestClient RestClient;
        private Logger Logger = LogManager.GetLogger("MOZABIQ");

        public async Task<PlayerInfoResult?> GetPlayerInfoByNameAsync(string name)
        {
            var request = new RestRequest($"/bridge?auth={auth.Token}&player={name}&platform=PC&enableClubsBeta=1&merge=1", Method.Get);
            request.AddHeader("Accept", "*/*");
            Logger.Info($"Querying Player info: {name}");
            var response = await RestClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Logger.Info($"Querying Player Info Successful: {name}");
                return JsonConvert.DeserializeObject<PlayerInfoResult>(response.Content!);
            }
            else
            {
                Logger.Error($"Querying Player Info Failed: {name}");
                return null;
            }
        }
        public async Task<PlayerInfoResult?> GetPlayerInfoByUIDAsync(string uid)
        {
            var request = new RestRequest($"/bridge?auth={auth.Token}&uid={uid}&platform=PC&enableClubsBeta=1&merge=1");
            request.AddHeader("Accept", "*/*");
            Logger.Info($"Querying Player info: {uid}");
            var response = await RestClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Logger.Info($"Querying Player Info Successful: {uid}");
                return JsonConvert.DeserializeObject<PlayerInfoResult>(response.Content!);
            }
            else
            {
                Logger.Error($"Querying Player Info Failed: {uid}");
                return null;
            }
        }
        public async Task<PredatorResult?> GetPredatorAsync()
        {
            var request = new RestRequest($"/predator?auth={auth.Token}");
            request.AddHeader("Accept", "*/*");
            Logger.Info($"Querying Predator");
            var response = await RestClient.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Logger.Info($"Querying Predator Successful");
                return JsonConvert.DeserializeObject<PredatorResult>(response.Content!);
            }
            else
            {
                Logger.Error($"Querying Predator Failed");
                return null;
            }
        }
    }

    internal class Auth
    {
        [JsonProperty("url")]
        public string Url { get; set; } = "url";
        [JsonProperty("token")]
        public string Token { get; set; } = "token";
    }
}
