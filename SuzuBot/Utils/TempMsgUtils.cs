using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SuzuBot.Utils;
public static class TempMsgUtils
{
    private static HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://tmhs.akulak.icu/")
    };

    public static async Task<TempMsgResult?> PostTempMsg(IEnumerable<TempMsgContent> messages)
    {
        var result = await _httpClient.PostAsJsonAsync("post", messages.ToArray());
        return await result.Content.ReadFromJsonAsync<TempMsgResult>();
    }
}

public enum MessageType
{
    Text,
    Image
}

public record TempMsgContent
{
    public MessageType Type { get; set; }
    public string Content { get; set; }
}

public record TempMsgResult
{
    public string Uuid { get; set; }
    public DateTime Expire { get; set; }

    [JsonIgnore]
    public string Url => $"https://tmhs.akulak.icu/?uuid={Uuid}";
}