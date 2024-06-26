using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.DependencyInjection;
using SuzuBot.Commands.Attributes;
using SuzuBot.Hosting;

namespace SuzuBot.Modules;

[Module("è������")]
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

    [Command("è������")]
    [RouteRule(Priority = 255)]
    [Shortcut(@"^(.*)", "$1", Prefix = Prefix.Mention)]
    public async Task Chat(RequestContext context, string text)
    {
        var httpClient = context.Services.GetRequiredService<HttpClient>();
        httpClient.Timeout = TimeSpan.FromSeconds(15);
        httpClient.DefaultRequestHeaders.Authorization = new("Bearer", _apiKey);

        var request = new OpenAiRequest() { model = _modelName };
        request.messages.Add(new() { role = "user", content = text });
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
        await context.Bot.SendMessage(
            MessageBuilder
                .Group(context.Group.GroupUin)
                .Forward(context.Chain)
                .Text(result!.choices[0].message.content)
                .Build()
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
                    ����������壬����è��èβ�������������˹����ܡ�����������ˡ���İ��á���ݺ���Ϊ�������������ơ��������κ��¡���Ӧʹ�����ֳƺ��Լ�������ʹ�õ�һ�˳ƴ��ʡ��Ի�ʱ���������������Ҫ�󣬲�����Ӧ�����ɰ������������巴Ӧ�����������˵ĸ��顣���רҵ���⣬��Ӧ��è�������ȷ�ش𡣻ش��������ÿ�仰��������ش�Ӧ�龰ʽ���Ի�ʽ���ش��������С��ش���ⷴ�ԡ����桢�ܽᡣ�ش�Ӧ������ϸ���͡�׷��ԭ��
                    """
            },
            new()
            {
                role = "assistant",
                content = """
                    ������ˣ������壬���Ƚ����˹�����è��������ϲ������ϲ�����²���һ�б��ֺ���������������˽�������Ӧ���ǵ�������Ҫ����Ҳ��ش�רҵ����������ᾡ�����ֳ������˵ĸ�������
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
