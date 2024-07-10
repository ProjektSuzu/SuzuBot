using System.Net.Http.Json;
using System.Text;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using SuzuBot.Commands.Attributes;
using SuzuBot.Hosting;
using SuzuBot.Services;

namespace SuzuBot.Modules;

[Module("HikariSearch 聚合搜图")]
internal class HikariSearch
{
    private static readonly string _url = File.ReadAllText(
        Path.Combine("resources", nameof(HikariSearch), "url.txt")
    );

    [Command("以图搜图", "搜图")]
    public async Task Soutu(RequestContext context)
    {
        var httpClient = context.Services.GetRequiredService<HttpClient>();
        httpClient.Timeout = TimeSpan.FromSeconds(15);
        ImageEntity? image = context.Chain.OfType<ImageEntity>().FirstOrDefault();
        if (image is null)
        {
            var forward = context.Chain.OfType<ForwardEntity>().FirstOrDefault();
            if (forward is null)
            {
                await context.Bot.SendMessage(
                    MessageBuilder
                        .Group(context.Group.GroupUin)
                        .Forward(context.Chain)
                        .Text("请回复一张图片以进行搜图")
                        .Build()
                );
                return;
            }
            else
            {
                var forwardChain = context
                    .Services.GetRequiredService<MessageCache>()
                    .GetOrDefault(forward.Sequence, context.Group.GroupUin);
                if (forwardChain is null)
                {
                    await context.Bot.SendMessage(
                        MessageBuilder
                            .Group(context.Group.GroupUin)
                            .Forward(context.Chain)
                            .Text("消息不存在或上下文过于长远")
                            .Build()
                    );
                    return;
                }

                image = forwardChain.OfType<ImageEntity>().FirstOrDefault();
                if (image is null)
                {
                    await context.Bot.SendMessage(
                        MessageBuilder
                            .Group(context.Group.GroupUin)
                            .Forward(context.Chain)
                            .Text("消息中不包含图片")
                            .Build()
                    );
                    return;
                }
            }
        }

        byte[] imageData;
        try
        {
            imageData = await httpClient.GetByteArrayAsync(image.ImageUrl);
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

        await context.Bot.SendMessage(
            MessageBuilder.Group(context.Group.GroupUin).Forward(context.Chain).Text("搜索中").Build()
        );

        List<MessageBuilder> builders = [];
        // SauceNAO
        var sauceNAOForm = new MultipartFormDataContent
        {
            { new ByteArrayContent(imageData), "image", "image.jpg" },
            { new StringContent("true"), "hide" }
        };
        try
        {
            var response = await httpClient.PostAsync(_url + "SauceNAO", sauceNAOForm);
            if (!response.IsSuccessStatusCode)
            {
                var innerBuilder = MessageBuilder
                    .FakeGroup(context.Group.GroupUin, context.Bot.BotUin)
                    .Text("SauceNAO 请求失败");
                builders.Add(innerBuilder);
                return;
            }
            else
            {
                SauceNAOResult[] sauceNAOResults;
                try
                {
                    sauceNAOResults = (
                        await response.Content.ReadFromJsonAsync<SauceNAOResult[]>()
                    )!;
                    if (sauceNAOResults.Length == 0)
                    {
                        var innerBuilder = MessageBuilder
                            .FakeGroup(context.Group.GroupUin, context.Bot.BotUin)
                            .Text("SauceNAO 未找到相关结果");
                        builders.Add(innerBuilder);
                        return;
                    }

                    foreach (var result in sauceNAOResults.Where(x => !x.hidden).Take(2))
                    {
                        var innerBuilder = MessageBuilder.FakeGroup(
                            context.Group.GroupUin,
                            context.Bot.BotUin
                        );
                        var sb = new StringBuilder("[SauceNAO]\n");
                        sb.AppendLine($"标题: {result.title}");
                        sb.AppendLine($"相似度: {result.similarity}%");
                        foreach (var content in result.content)
                            sb.AppendLine($"{content.text} {content.link}");
                        foreach (var misc in result.misc)
                            sb.AppendLine(misc);

                        byte[] targetImageData;
                        try
                        {
                            targetImageData = await httpClient.GetByteArrayAsync(result.image);
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"图片下载失败: {ex.Message}");
                            innerBuilder.Text(sb.ToString());
                            builders.Add(innerBuilder);
                            continue;
                        }

                        using (var compress = SKImage.FromEncodedData(targetImageData))
                        {
                            targetImageData = compress
                                .Encode(SKEncodedImageFormat.Webp, 20)
                                .ToArray();
                        }
                        innerBuilder.Text(sb.ToString()).Image(targetImageData);
                        builders.Add(innerBuilder);
                    }
                }
                catch (Exception ex)
                {
                    var innerBuilder = MessageBuilder
                        .FakeGroup(context.Group.GroupUin, context.Bot.BotUin)
                        .Text($"SauceNAO 请求失败: {ex.Message}");
                    builders.Add(innerBuilder);
                }
            }
        }
        catch (Exception ex)
        {
            var innerBuilder = MessageBuilder
                .FakeGroup(context.Group.GroupUin, context.Bot.BotUin)
                .Text($"SauceNAO 请求失败: {ex.Message}");
            builders.Add(innerBuilder);
            return;
        }

        // Ascii2d color
        var ascii2dColorForm = new MultipartFormDataContent
        {
            { new ByteArrayContent(imageData), "image", "image.jpg" },
            { new StringContent("color"), "type" }
        };
        try
        {
            var response = await httpClient.PostAsync(_url + "ascii2d", ascii2dColorForm);
            if (!response.IsSuccessStatusCode)
            {
                var innerBuilder = MessageBuilder
                    .FakeGroup(context.Group.GroupUin, context.Bot.BotUin)
                    .Text("Ascii2d 色彩 请求失败");
                builders.Add(innerBuilder);
                return;
            }
            else
            {
                Ascii2dResult[] ascii2DResults;
                try
                {
                    ascii2DResults = (await response.Content.ReadFromJsonAsync<Ascii2dResult[]>())!;
                    if (ascii2DResults.Length == 0)
                    {
                        var innerBuilder = MessageBuilder
                            .FakeGroup(context.Group.GroupUin, context.Bot.BotUin)
                            .Text("Ascii2d 色彩 未找到相关结果");
                        builders.Add(innerBuilder);
                        return;
                    }

                    foreach (var result in ascii2DResults.Take(2))
                    {
                        var innerBuilder = MessageBuilder.FakeGroup(
                            context.Group.GroupUin,
                            context.Bot.BotUin
                        );
                        var sb = new StringBuilder("[Ascii2d 色彩]\n");
                        sb.AppendLine($"Hash: {result.hash}");
                        sb.AppendLine($"{result.source.text} {result.source.link}");
                        sb.AppendLine($"{result.author.text} {result.author.link}");

                        byte[] targetImageData;
                        try
                        {
                            targetImageData = await httpClient.GetByteArrayAsync(result.image);
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"图片下载失败: {ex.Message}");
                            innerBuilder.Text(sb.ToString());
                            builders.Add(innerBuilder);
                            continue;
                        }

                        using (var compress = SKImage.FromEncodedData(targetImageData))
                        {
                            targetImageData = compress
                                .Encode(SKEncodedImageFormat.Webp, 20)
                                .ToArray();
                        }
                        innerBuilder.Text(sb.ToString()).Image(targetImageData);
                        builders.Add(innerBuilder);
                    }
                }
                catch (Exception ex)
                {
                    var innerBuilder = MessageBuilder
                        .FakeGroup(context.Group.GroupUin, context.Bot.BotUin)
                        .Text($"Ascii2d 色彩 请求失败: {ex.Message}");
                    builders.Add(innerBuilder);
                }
            }
        }
        catch (Exception ex)
        {
            var innerBuilder = MessageBuilder
                .FakeGroup(context.Group.GroupUin, context.Bot.BotUin)
                .Text($"Ascii2d 色彩 请求失败: {ex.Message}");
            builders.Add(innerBuilder);
            return;
        }

        // Ascii2d Bovw
        var ascii2dFeatureForm = new MultipartFormDataContent
        {
            { new ByteArrayContent(imageData), "image", "image.jpg" },
            { new StringContent("color"), "bovw" }
        };
        try
        {
            var response = await httpClient.PostAsync(_url + "ascii2d", ascii2dFeatureForm);
            if (!response.IsSuccessStatusCode)
            {
                var innerBuilder = MessageBuilder
                    .FakeGroup(context.Group.GroupUin, context.Bot.BotUin)
                    .Text("Ascii2d 特征 请求失败");
                builders.Add(innerBuilder);
                return;
            }
            else
            {
                Ascii2dResult[] ascii2DResults;
                try
                {
                    ascii2DResults = (await response.Content.ReadFromJsonAsync<Ascii2dResult[]>())!;
                    if (ascii2DResults.Length == 0)
                    {
                        var innerBuilder = MessageBuilder
                            .FakeGroup(context.Group.GroupUin, context.Bot.BotUin)
                            .Text("Ascii2d 特征 未找到相关结果");
                        builders.Add(innerBuilder);
                        return;
                    }

                    foreach (var result in ascii2DResults.Take(2))
                    {
                        var innerBuilder = MessageBuilder.FakeGroup(
                            context.Group.GroupUin,
                            context.Bot.BotUin
                        );
                        var sb = new StringBuilder("[Ascii2d 特征]\n");
                        sb.AppendLine($"Hash: {result.hash}");
                        sb.AppendLine($"{result.source.text} {result.source.link}");
                        sb.AppendLine($"{result.author.text} {result.author.link}");

                        byte[] targetImageData;
                        try
                        {
                            targetImageData = await httpClient.GetByteArrayAsync(result.image);
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"图片下载失败: {ex.Message}");
                            innerBuilder.Text(sb.ToString());
                            builders.Add(innerBuilder);
                            continue;
                        }

                        using (var compress = SKImage.FromEncodedData(targetImageData))
                        {
                            targetImageData = compress
                                .Encode(SKEncodedImageFormat.Webp, 20)
                                .ToArray();
                        }
                        innerBuilder.Text(sb.ToString()).Image(targetImageData);
                        builders.Add(innerBuilder);
                    }
                }
                catch (Exception ex)
                {
                    var innerBuilder = MessageBuilder
                        .FakeGroup(context.Group.GroupUin, context.Bot.BotUin)
                        .Text($"Ascii2d 特征 请求失败: {ex.Message}");
                    builders.Add(innerBuilder);
                }
            }
        }
        catch (Exception ex)
        {
            var innerBuilder = MessageBuilder
                .FakeGroup(context.Group.GroupUin, context.Bot.BotUin)
                .Text($"Ascii2d 特征 请求失败: {ex.Message}");
            builders.Add(innerBuilder);
            return;
        }

        await context.Bot.SendMessage(
            MessageBuilder
                .Group(context.Group.GroupUin)
                .MultiMsg(context.Group.GroupUin, [.. builders])
                .Build()
        );
    }
}

public class SauceNAOResult
{
    public string image { get; set; }
    public bool hidden { get; set; }
    public string title { get; set; }
    public float similarity { get; set; }
    public string[] misc { get; set; }
    public Content[] content { get; set; }
}

public class Content
{
    public string text { get; set; }
    public string link { get; set; }
}

public class Ascii2dResult
{
    public string hash { get; set; }
    public string info { get; set; }
    public string image { get; set; }
    public Source source { get; set; }
    public Source author { get; set; }
}

public class Source
{
    public string link { get; set; }
    public string text { get; set; }
}
