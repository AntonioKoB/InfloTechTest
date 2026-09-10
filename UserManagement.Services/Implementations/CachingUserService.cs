using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Caching;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

/// <summary>
/// Cache-aside decorator for a single user by id. It sits beneath AuditingUserService on purpose, so a read
/// served from memory is still recorded as a view by the layer above - the reason this cache lives in the
/// service layer rather than at the HTTP layer, where a hit never reaches the audit. The cache holds and
/// hands out copies: the API's update flow mutates the fetched user in place before saving it, and on a
/// miss the tracked entity must be the one returned, so the copy is what goes into the cache. Update and
/// Delete invalidate whether or not they succeed - a failed save can mean the row changed or vanished
/// underneath, and the cost is one extra read. Not cached here: the lists (cached at the HTTP layer by the
/// API's output caching), the email lookup (uniqueness checks and sign-in must see the database), and
/// misses (an id that does not exist now may after the next create).
/// </summary>
public class CachingUserService : IUserService
{
    // The safety net behind explicit invalidation, not the primary mechanism.
    private static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(5);

    private readonly IUserService _inner;
    private readonly ICache _cache;

    public CachingUserService(IUserService inner, ICache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<IEnumerable<User>> FilterByActiveAsync(bool isActive) => _inner.FilterByActiveAsync(isActive);
    public Task<IEnumerable<User>> GetAllAsync() => _inner.GetAllAsync();
    public Task<User?> GetByEmailAsync(string email) => _inner.GetByEmailAsync(email);
    public Task CreateAsync(User user) => _inner.CreateAsync(user);

    public async Task<User?> GetByIdAsync(long id, bool recordAsViewed = false)
    {
        var cached = await _cache.GetAsync<User>(Key(id));
        if (cached is not null)
        {
            return cached.Clone();
        }

        var user = await _inner.GetByIdAsync(id, recordAsViewed);
        if (user is not null)
        {
            await _cache.SetAsync(Key(id), user.Clone(), TimeToLive);
        }

        return user;
    }

    public async Task UpdateAsync(User user)
    {
        try
        {
            await _inner.UpdateAsync(user);
        }
        finally
        {
            await _cache.RemoveAsync(Key(user.Id));
        }
    }

    public async Task DeleteAsync(long id)
    {
        try
        {
            await _inner.DeleteAsync(id);
        }
        finally
        {
            await _cache.RemoveAsync(Key(id));
        }
    }

    private static string Key(long id) => $"users:{id}";
}
