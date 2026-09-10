using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace UserManagement.Services.Caching;

public class MemoryCacheAdapter : ICache
{
    public MemoryCacheAdapter(IMemoryCache cache)
    {
    }

    public Task<T?> GetAsync<T>(string key) where T : class => throw new NotImplementedException();

    public Task SetAsync<T>(string key, T value, TimeSpan timeToLive) where T : class => throw new NotImplementedException();

    public Task RemoveAsync(string key) => throw new NotImplementedException();
}
