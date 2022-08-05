using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RestSharp;

namespace RinBot.Command.Bilibili
{
    internal class IngredientCheck
    {
        #region Singleton
        private static IngredientCheck instance;
        public static IngredientCheck Instance
        {
            get
            {
                if (instance == null) instance = new();
                return instance;
            }
        }
        private IngredientCheck()
        {
            InitVtbLists();
        }
        #endregion

        private static RestClient bilibiliAPI = new RestClient("https://api.bilibili.com/");
        private static RestClient vtbAPI = new RestClient("https://api.tokyo.vtbs.moe/");
        private static List<uint> vtbs = new();
        private static Logger Logger = LogManager.GetLogger("查成分");
        public static void InitVtbLists()
        {
            vtbs = new List<uint>();
            var request = new RestRequest("/v1/vtbs");
            var responseJson = vtbAPI.Get(request);
            var response = JsonConvert.DeserializeObject<List<VtbInfo>>(responseJson.Content);
            response.ForEach(x => vtbs.Add(x.MemberId));
            vtbs.Sort();
            Logger.Info($"Added {response.Count} Vtb(s).");
        }

        public string? GetUserName(int uid)
        {
            var request = new RestRequest($"x/space/acc/info?mid={uid}");
            var responseJson = bilibiliAPI.Get(request);
            var response = JsonConvert.DeserializeObject<JObject>(responseJson.Content);
            if (response == null || (int)response["code"] != 0)
                return null;
            return response["data"]?["name"]?.Value<string>() ?? null;
        }

        public List<VtbInfo>? GetUserFollowings(int uid)
        {
            List<VtbInfo>? userFollowings = new();
            int page = 1;
            for (; page <= 5; page++)
            {
                Logger.Info($"Get Page {page}/5.");
                var request = new RestRequest($"x/relation/followings?vmid={uid}&order_type=attention&pn={page}");
                var responseJson = bilibiliAPI.Get(request);
                var response = JsonConvert.DeserializeObject<JObject>(responseJson.Content);
                if (response == null || (int)response["code"] != 0)
                    return null;
                var followings = response["data"]?["list"]?.ToList().Select(x => new VtbInfo() { MemberId = (uint)x["mid"], UserName = x["uname"].ToString() }).ToList();
                userFollowings.AddRange(followings);
            }
            return userFollowings;
        }

        public IngredientResult? Check(int userId)
        {
            var userName = GetUserName(userId);
            var followingList = GetUserFollowings(userId);
            var vtbList = followingList?.Where(x => vtbs.BinarySearch(x.MemberId) >= 0).ToList() ?? null;
            return new()
            {
                UserName = userName,
                Ingredients = vtbList,
            };
        }

        public IngredientResult? Check(string userName)
        {
            var request = new RestRequest($"x/web-interface/search/type?search_type=bili_user&keyword={userName}");
            var responseJson = bilibiliAPI.Get(request);
            var response = JsonConvert.DeserializeObject<JObject>(responseJson.Content);
            if (response == null || (int)response["code"] != 0)
                return null;
            var target = response["data"]["result"]?.ToList().First()["mid"].Value<int>() ?? -1;
            if (target == -1) return null;
            else return Check(target);
        }


    }

    public class VtbInfo
    {
        [JsonProperty("mid")]
        public uint MemberId { get; set; }
        public string UserName { get; set; }
    }

    public class IngredientResult
    {
        public string UserName { get; set; }
        public List<VtbInfo> Ingredients { get; set; }
    }
}
