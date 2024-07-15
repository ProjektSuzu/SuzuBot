using System.Runtime.Caching;
using Lagrange.Core.Message;

namespace SuzuBot.Services;

internal class MessageCache
{
    private readonly ObjectCache _memoryCache = MemoryCache.Default;

    public MessageCache() { }

    public void Add(MessageChain chain)
    {
        string key = $"{chain.Sequence}@{chain.GroupUin}";
        CacheItemPolicy policy = new() { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5) };
        _memoryCache.Add(key, chain, policy);
    }

    public MessageChain? GetOrDefault(uint sequence, uint groupUin)
    {
        string key = $"{sequence}@{groupUin}";
        return _memoryCache.Get(key) as MessageChain;
    }
}
