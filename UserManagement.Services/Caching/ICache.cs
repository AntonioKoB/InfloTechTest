using System;
using System.Threading.Tasks;

namespace UserManagement.Services.Caching;

/// <summary>
/// The cache contract the service layer depends on, so the store can be swapped: in-process today,
/// distributed later. A distributed implementation must serialize PasswordHash despite its [JsonIgnore].
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
