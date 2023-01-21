using System.Reflection;
using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using SuzuBot.Core.Attributes;
using SuzuBot.Core.EventArgs.Message;
using SuzuBot.Core.Modules;
using SuzuBot.Utils;

namespace SuzuBot.Modules.Entertaining;
public class OTTOModule : BaseModule
{
    private HttpClient _httpClient = new HttpClient()
    {
        BaseAddress = new Uri(@"https://www.aolianfeiallin.top/"),
        Timeout = TimeSpan.FromSeconds(60)
    };

    public OTTOModule()
    {
        Name = "电棍活字印刷";
    }

    [Command("说的道理", "^otto (.*)")]
    public async Task OttoTTS(MessageEventArgs eventArgs, string[] args)
    {
        var builder = new MessageBuilder();
        var audio = await GetTTS(args[0]);
        if (audio is null)
        {
            builder.Text("生成失败");
            await eventArgs.SendMessage(builder);
            return;
        }
        builder.Add(RecordChain.Create(audio));
        await eventArgs.SendMessage(builder);
    }

    private async Task<byte[]?> GetTTS(string text)
    {
        var post = new MultipartFormDataContent
        {
            { new StringContent(text), "text" },
            { new StringContent("true"), "inYsddMode" },
            { new StringContent("true"), "norm" },
            { new StringContent("false"), "reverse" },
            { new StringContent("1"), "speedMult" },
            { new StringContent("1"), "pitchMult" },
        };
        try
        {
            var result = await _httpClient.PostAsync("make", post);
            if (!result.IsSuccessStatusCode)
                return null;

            var id = result.Content.ReadAsStringAsync().Result.DeserializeJson<TTSResponse>().id;
            var bytes = await _httpClient.GetByteArrayAsync($"get/{id}.mp3");
            return bytes;
        }
        catch
        {
            return null;
        }

    }

}


public class TTSResponse
{
    public int code { get; set; }
    public string id { get; set; }
}
