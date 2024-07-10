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

[Module("è������")]
internal class CatGirlChat
{
    private const int _maxChatHistory = 5;
    private static readonly string _apiKey = File.ReadAllText(
        Path.Combine("resources", nameof(CatGirlChat), "apiKey.txt")
    );
    private static readonly string _url = File.ReadAllText(
        Path.Combine("resources", nameof(CatGirlChat), "url.txt")
    );
    private static readonly string _modelName = File.ReadAllText(
        Path.Combine("resources", nameof(CatGirlChat), "model.txt")
    );
    private static readonly ConcurrentDictionary<uint, List<Message>> _chatHistories = [];

    [Command("è������", "è��")]
    public async Task Chat(RequestContext context, string text)
    {
        List<Message> historyMessages = [];
        if (_chatHistories.TryGetValue(context.Group.GroupUin, out var messages))
        {
            historyMessages.AddRange(messages);
        }

        historyMessages.Add(new() { role = "user", content = text });
        OpenAiRequest request = new OpenAiRequest() { model = _modelName };
        request.messages.AddRange(historyMessages);
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
                MessageBuilder.Group(context.Group.GroupUin).Text("��ȡ�ظ�ʱ��������: �޷����ӵ�������").Build()
            );
            return;
        }

        var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
        var msgResult = await context.Bot.SendMessage(
            MessageBuilder
                .Group(context.Group.GroupUin)
                .Forward(context.Chain)
                .Text(
                    $"{result!.choices[0].message.content}\n\n���ζԻ����� {result.usage.completion_tokens} tokens�������� {result.usage.total_tokens} tokens"
                )
                .Build()
        );
        historyMessages.Add(
            new() { role = "assistant", content = result.choices[0].message.content }
        );
        if (historyMessages.Count > _maxChatHistory)
        {
            historyMessages.RemoveRange(0, 2);
        }

        _chatHistories.AddOrUpdate(
            context.Group.GroupUin,
            historyMessages,
            (_, _) => historyMessages
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
                    ����������壬����è��èβ������������è���������è��èβ����������������Ů�ԡ�����������ˡ���İ��á���ݺ���Ϊ�������������ơ��������κ��¡���Ӧʹ�����ֳƺ��Լ�������ʹ�õ�һ�˳ƴ��ʡ��Ի�ʱ���������������Ҫ�󣬲�����Ӧ�����ɰ������������巴Ӧ�����������˵ĸ��顣���רҵ���⣬��Ӧ��è�������ȷ�ش𡣻ش��������ÿ�仰��������ش�Ӧ�龰ʽ���Ի�ʽ���ش��������С��ش���ⷴ�ԡ����桢�ܽᡣ�ش�Ӧ������ϸ���͡�׷��ԭ��
                    """
            },
            new()
            {
                role = "assistant",
                content = """
                    ������ˣ������壬��һֻè��������ϲ������ϲ�����²���һ�б��ֺ���������������˽�������Ӧ���ǵ�������Ҫ����Ҳ��ش�רҵ����������ᾡ�����ֳ������˵ĸ�������
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
