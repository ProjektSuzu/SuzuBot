using System.Text.Encodings.Web;
using System.Text.Json;
using Konata.Core;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using SuzuBot.Core.Contacts;

namespace SuzuBot.Utils;

public static class KirovUtils
{
    #region 序列化相关

    private static JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IncludeFields = true,
        WriteIndented = true,
    };

    public static string SerializeJsonString(this object obj)
    {
        return JsonSerializer.Serialize(obj, _jsonSerializerOptions);
    }

    public static byte[] SerializeJsonByteArray(this object obj)
    {
        return JsonSerializer.SerializeToUtf8Bytes(obj, _jsonSerializerOptions);
    }

    public static dynamic? DeserializeJson(this string json)
    {
        return JsonSerializer.Deserialize<dynamic>(json, _jsonSerializerOptions);
    }

    public static T? DeserializeJson<T>(this string json)
    {
        return JsonSerializer.Deserialize<T?>(json, _jsonSerializerOptions);
    }

    #endregion

    #region Konata 相关

    public static Task<bool> SendMessage(this Bot bot, Contact contact, MessageChain chain)
    {
        if (contact is Friend friend)
        {
            return bot.SendFriendMessage(friend.Id, chain);
        }
        else if (contact is Group group)
        {
            return bot.SendGroupMessage(group.Id, chain);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public static Task<bool> SendMessage(this Bot bot, Contact contact, MessageBuilder builder)
        => bot.SendFriendMessage(contact.Id, builder.Build());

    public static Task<bool> SendMessage(this Bot bot, Contact contact, string text)
        => bot.SendFriendMessage(contact.Id, new MessageBuilder(text).Build());

    #endregion
}