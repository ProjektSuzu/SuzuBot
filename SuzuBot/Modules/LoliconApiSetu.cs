using System.Net.Http.Json;
using System.Text;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using SuzuBot.Commands.Attributes;
using SuzuBot.Hosting;
using SuzuBot.Services;

namespace SuzuBot.Modules;

[Module("LoliconAPI 色图")]
internal class LoliconApiSetu
{
    [Command("LoliconAPI 色图", "色图")]
    public async Task Setu(RequestContext context, string keyword = "")
    {
        var httpClient = context.Services.GetRequiredService<HttpClient>();
        httpClient.Timeout = TimeSpan.FromSeconds(15);
        var queryUrl = $"https://api.lolicon.app/setu/v2?keyword={keyword}&excludeAI=true";
        LoliconApiResult? result;
        try
        {
            var httpContent = await httpClient.GetAsync(queryUrl);
            if (!httpContent.IsSuccessStatusCode)
            {
                await context.Bot.SendMessage(
                    MessageBuilder.Group(context.Group.GroupUin).Text("获取图片时发生错误: 无法连接到服务器").Build()
                );
                return;
            }

            result = await httpContent.Content.ReadFromJsonAsync<LoliconApiResult>();
            if (!string.IsNullOrEmpty(result.error))
            {
                await context.Bot.SendMessage(
                    MessageBuilder
                        .Group(context.Group.GroupUin)
                        .Text($"获取图片时发生错误: {result.error}")
                        .Build()
                );
                return;
            }
            else if (result.data.Length == 0)
            {
                await context.Bot.SendMessage(
                    MessageBuilder.Group(context.Group.GroupUin).Text($"未找到图片").Build()
                );
                return;
            }
        }
        catch (Exception ex)
        {
            await context.Bot.SendMessage(
                MessageBuilder
                    .Group(context.Group.GroupUin)
                    .Text($"获取图片时发生错误: {ex.Message}")
                    .Build()
            );
            return;
        }

        var data = result.data[0];
        var sb = new StringBuilder();
        sb.AppendLine($"标题: {data.title} ({data.pid})");
        sb.AppendLine($"作者: {data.author} ({data.uid})");
        sb.AppendLine($"标签: {string.Join(' ', data.tags)}");
        byte[] imgData;
        string path = Path.Combine("resources", nameof(LoliconApiSetu), $"{data.pid}.webp");
        if (!File.Exists(path))
        {
            try
            {
                imgData = await httpClient.GetByteArrayAsync(data.urls.original);
                using (var compress = SKImage.FromEncodedData(imgData))
                {
                    imgData = compress.Encode(SKEncodedImageFormat.Webp, 80).ToArray();
                }
                File.WriteAllBytes(path, imgData);
            }
            catch (Exception ex)
            {
                await context.Bot.SendMessage(
                    MessageBuilder
                        .Group(context.Group.GroupUin)
                        .Text($"下载图片时发生错误: {ex.Message}")
                        .Build()
                );
                return;
            }
        }
        else
            imgData = File.ReadAllBytes(path);

        var msgResult = await context.Bot.SendMessage(
            MessageBuilder.Group(context.Group.GroupUin).Text(sb.ToString()).Image(imgData).Build()
        );
        await Task.Delay(30000);
        await context.Bot.RecallGroupMessage(context.Group.GroupUin, msgResult);
    }
}

public class LoliconApiResult
{
    public string error { get; set; }
    public Datum[] data { get; set; }
}

public class Datum
{
    public int pid { get; set; }
    public int p { get; set; }
    public int uid { get; set; }
    public string title { get; set; }
    public string author { get; set; }
    public bool r18 { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public string[] tags { get; set; }
    public string ext { get; set; }
    public int aiType { get; set; }
    public long uploadDate { get; set; }
    public Urls urls { get; set; }
}

public class Urls
{
    public string original { get; set; }
}
