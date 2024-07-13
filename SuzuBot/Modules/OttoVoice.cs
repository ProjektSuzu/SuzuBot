using System.Web;
using Konata.Codec.Audio;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Message;
using Microsoft.Extensions.DependencyInjection;
using SuzuBot.Commands.Attributes;
using SuzuBot.Hosting;

namespace SuzuBot.Modules;

[Module("电棍语音")]
internal class OttoVoice
{
    private const string _url =
        " http://127.0.0.1:23456/voice/bert-vits2?id=0&format=wav&text={text}";

    [Command("电棍语音", "电棍")]
    public async Task Otto(RequestContext context, string text)
    {
        var url = _url.Replace("{text}", HttpUtility.UrlEncode(text));
        var httpClient = context.Services.GetRequiredService<HttpClient>();
        var resultStream = await httpClient.GetStreamAsync(url);
        var wavStream = new MemoryStream();
        await resultStream.CopyToAsync(wavStream);
        var silkData = new MemoryStream();
        wavStream.Seek(0, SeekOrigin.Begin);
        using var pipeline = new AudioPipeline
        {
            wavStream,
            new AudioResampler(AudioInfo.SilkV3()),
            silkData,
        };
        await pipeline.Start();
        wavStream.Seek(0, SeekOrigin.Begin);
        var duration = GetWavDuration(wavStream);
        await context.Bot.SendMessage(
            MessageBuilder
                .Group(context.Group.GroupUin)
                .Record(silkData.ToArray(), (int)duration)
                .Build()
        );
    }

    private static double GetWavDuration(Stream stream)
    {
        using (BinaryReader reader = new BinaryReader(stream))
        {
            // 跳过前20个字节（RIFF标识符、文件大小、WAVE标识符、fmt 标识符）
            reader.ReadBytes(20);

            // 读取格式块中相关的头信息
            int audioFormat = reader.ReadInt16(); // 音频格式
            int numChannels = reader.ReadInt16(); // 声道数
            int sampleRate = reader.ReadInt32(); // 采样率

            reader.ReadBytes(6); // 跳过一些字段（字节率、块对齐）

            int bitsPerSample = reader.ReadInt16(); // 位深度

            // 跳过到数据块头
            while (new string(reader.ReadChars(4)) != "data")
            {
                int chunkSize = reader.ReadInt32();
                reader.ReadBytes(chunkSize);
            }

            int dataSize = reader.ReadInt32(); // 数据块大小

            // 计算时长
            int bytesPerSample = bitsPerSample / 8;
            int totalSamples = dataSize / (numChannels * bytesPerSample);
            double duration = (double)totalSamples / sampleRate;

            return duration;
        }
    }
}
