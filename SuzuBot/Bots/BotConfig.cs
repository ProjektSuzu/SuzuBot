using System.Text.Json.Serialization;
using Konata.Core.Common;

namespace SuzuBot.Bots;

internal class BotConfig
{
    [JsonRequired]
    public string Uin { get; set; }
    public string? Password { get; set; }
    public Konata.Core.Common.BotConfig? Config { get; set; }
    public BotDevice? Device { get; set; }
    public BotKeyStore? KeyStore { get; set; }
}
