using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace UserManagement.Services.Caching;

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
