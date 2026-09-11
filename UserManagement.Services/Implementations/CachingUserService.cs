using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Caching;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Implementations;

/// <summary>
/// Cache-aside for a single user by id, beneath AuditingUserService so a hit is still audited. Hands out
/// copies, since callers mutate the fetched user in place; Update and Delete invalidate whether or not they
/// succeed; misses, lists and the email lookup are not cached.
/// </summary>
public class CachingUserService : IUserService
{
    // Safety net behind explicit invalidation.
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
