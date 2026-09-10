using System;
using System.Threading.Tasks;

namespace UserManagement.Services.Caching;

public interface ICache
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan timeToLive) where T : class;
    Task RemoveAsync(string key);
}
