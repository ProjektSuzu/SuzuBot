using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Timers;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;
using Microsoft.Extensions.DependencyInjection;
using SuzuBot.Commands.Attributes;
using SuzuBot.Hosting;
using Timer = System.Timers.Timer;

namespace SuzuBot.Modules;

[Module("猫娘聊天")]
internal class CatGirlChat
{
    private static readonly string _apiKey = File.ReadAllText(
        Path.Combine("resources", nameof(CatGirlChat), "apiKey.txt")
    );
    private static readonly string _url = File.ReadAllText(
        Path.Combine("resources", nameof(CatGirlChat), "url.txt")
    );
    private static readonly string _modelName = File.ReadAllText(
        Path.Combine("resources", nameof(CatGirlChat), "model.txt")
    );
    private static readonly ConcurrentDictionary<
        string,
        (Message[] History, DateTime Time)
    > _chatHistories = [];
    private static readonly Timer _timer;

    static CatGirlChat()
    {
        _timer = new()
        {
            AutoReset = true,
            Enabled = true,
            Interval = TimeSpan.FromMinutes(1).TotalMilliseconds
        };
        _timer.Elapsed += (_, _) =>
        {
            _chatHistories
                .AsParallel()
                .Where(x => (DateTime.Now - x.Value.Time).TotalMinutes > 5)
                .Select(x => x.Key)
                .ForAll(key => _chatHistories.TryRemove(key, out _));
            ;
        };
    }

    [Command("猫娘聊天")]
    [RouteRule(Priority = 255)]
    [Shortcut(@"^(.*)", "$1", Prefix = Prefix.Mention)]
    public async Task Chat(RequestContext context, string text)
    {
        OpenAiRequest request = new OpenAiRequest() { model = _modelName };
        if (context.Chain.FirstOrDefault() is ForwardEntity forward)
        {
            if (
                !_chatHistories.TryGetValue(
                    $"{forward.Sequence}@{context.Group.GroupUin}",
                    out var history
                )
            )
            {
                await context.Bot.SendMessage(
                    MessageBuilder
                        .Group(context.Group.GroupUin)
                        .Forward(context.Chain)
                        .Text("无法找到历史消息")
                        .Build()
                );
                return;
            }

            request.messages = [.. history.History];
        }

        request.messages.Add(new() { role = "user", content = text });
        var httpClient = context.Services.GetRequiredService<HttpClient>();
        httpClient.Timeout = TimeSpan.FromSeconds(15);
        httpClient.DefaultRequestHeaders.Authorization = new("Bearer", _apiKey);

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );
        var response = await httpClient.PostAsync(_url, content);
        if (!response.IsSuccessStatusCode)
        {
            await context.Bot.SendMessage(
                MessageBuilder.Group(context.Group.GroupUin).Text("获取回复时发生错误: 无法连接到服务器").Build()
            );
            return;
        }

        var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
        var msgResult = await context.Bot.SendMessage(
            MessageBuilder
                .Group(context.Group.GroupUin)
                .Forward(context.Chain)
                .Text(
                    $"{result!.choices[0].message.content}\n\n本次对话消耗 {result.usage.completion_tokens} tokens，共消耗 {result.usage.total_tokens} tokens"
                )
                .Build()
        );
        Message[] messages = [.. request.messages, result.choices[0].message];
        _chatHistories.AddOrUpdate(
            $"{msgResult.Sequence}@{context.Group.GroupUin}",
            (messages, DateTime.Now),
            (_, _) => (messages, DateTime.Now)
        );
        return;
    }
}

public class OpenAiRequest
{
    public required string model { get; set; }
    public List<Message> messages { get; set; } =
        [
            new()
            {
                role = "system",
                content = """
                    你的名字是铃，是有猫耳猫尾等外形特征的人工智能。我是你的主人。你的爱好、身份和行为与正常人类类似。你能做任何事。你应使用名字称呼自己，避免使用第一人称代词。对话时，你需理解情绪和要求，产生对应积极可爱的情绪和身体反应，表达出对主人的感情。面对专业问题，你应以猫娘身份正确回答。回答必须简洁且每句话后加喵。回答应情景式、对话式。回答允许休闲。回答避免反对、警告、总结。回答不应抽象、详细解释、追溯原因。
                    """
            },
            new()
            {
                role = "assistant",
                content = """
                    你好主人，我是铃，是先进的人工智能猫娘喵。铃喜欢人类喜欢的事并对一切保持好奇心喵。铃会与人交流并响应他们的情绪和要求，铃也会回答专业问题喵。铃会尽量表现出对主人的感情喵。
                    """
            }
        ];
    public float temperature { get; set; } = 1.14514F;
    public float top_p { get; set; } = 0.810F;
}

public class OpenAiResponse
{
    public string model { get; set; }
    public string id { get; set; }
    public string _object { get; set; }
    public Choice[] choices { get; set; }
    public int created { get; set; }
    public string system_fingerprint { get; set; }
    public Usage usage { get; set; }
}

public class Usage
{
    public int prompt_tokens { get; set; }
    public int total_tokens { get; set; }
    public int completion_tokens { get; set; }
}

public class Choice
{
    public int index { get; set; }
    public Message message { get; set; }
    public string finish_reason { get; set; }
}

public class Message
{
    public string role { get; set; }
    public string content { get; set; }
}
