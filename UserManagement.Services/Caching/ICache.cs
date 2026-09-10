using System;
using System.Threading.Tasks;

namespace UserManagement.Services.Caching;

/// <summary>
/// The cache the service layer depends on, kept to the three operations a cache-aside decorator needs so
/// the store can be swapped without touching the callers: the in-process MemoryCacheAdapter today, a
/// distributed one (e.g. Redis over IDistributedCache) when the API runs on more than one instance. A
/// distributed implementation must serialize the whole entity: User.PasswordHash is [JsonIgnore]d for the
/// audit snapshots, so the default JSON contract would silently drop it from cached users.
/// </summary>
public interface ICache
{
    /// <summary>The cached value, or null when the key is absent or its time-to-live has passed.</summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>Stores the value, replacing any existing entry, for at most the given time-to-live.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan timeToLive) where T : class;

    /// <summary>Removes the entry if present; a no-op otherwise.</summary>
    Task RemoveAsync(string key);
}
