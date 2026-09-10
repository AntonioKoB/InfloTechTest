using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace UserManagement.Services.Caching;

/// <summary>
/// ICache over the framework's in-process IMemoryCache. Values are held by reference, so the decorator that
/// uses this cache is responsible for never handing a cached instance to a caller (see CachingUserService).
/// Per process by design: on a scaled-out deployment each instance has its own cache and its own
/// invalidation, which is what a distributed adapter would replace.
/// </summary>
public class MemoryCacheAdapter : ICache
{
    private readonly IMemoryCache _cache;

    public MemoryCacheAdapter(IMemoryCache cache) => _cache = cache;

    public Task<T?> GetAsync<T>(string key) where T : class
        => Task.FromResult(_cache.TryGetValue(key, out T? value) ? value : null);

    public Task SetAsync<T>(string key, T value, TimeSpan timeToLive) where T : class
    {
        _cache.Set(key, value, timeToLive);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}
