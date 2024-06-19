using System.Collections.Concurrent;
using Lagrange.Core.Message;
using Timer = System.Timers.Timer;

namespace SuzuBot.Services;

internal class MessageCache
{
    private readonly ConcurrentDictionary<
        string,
        (DateTime CachedTime, MessageChain Chain)
    > _chains;
    private readonly Timer _timer;

    public MessageCache()
    {
        _chains = [];
        _timer = new()
        {
            AutoReset = true,
            Enabled = true,
            Interval = TimeSpan.FromMinutes(1).TotalMilliseconds
        };
        _timer.Elapsed += (_, _) => FlushCache();
    }

    public void Add(MessageChain chain)
    {
        string key = $"{chain.Sequence}@{chain.GroupUin}";
        _chains.AddOrUpdate(key, (DateTime.Now, chain), (key, old) => (DateTime.Now, chain));
    }

    public MessageChain? GetOrDefault(uint sequence, uint groupUin)
    {
        string key = $"{sequence}@{groupUin}";
        return _chains.TryGetValue(key, out var value) ? value.Chain : null;
    }

    private void FlushCache()
    {
        var needRemoveKeys = _chains
            .AsParallel()
            .Where(x => (DateTime.Now - x.Value.CachedTime) >= TimeSpan.FromMinutes(5))
            .Select(x => x.Key);
        lock (_chains)
        {
            needRemoveKeys.AsParallel().ForAll(key => _chains.Remove(key, out _));
        }
    }
}
