using System;
using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Api.Caching;

/// <summary>
/// Response caching for the users list, the one read in this API that has no side effect. Each filter
/// value is its own cached response, tagged so a successful write can evict them all at once; the entry is
/// also time-boxed so anything that writes to the database around the API self-heals. A single user is
/// deliberately not cached here: that read is audited, so its cache lives beneath the audit in the service
/// layer (see CachingUserService).
/// </summary>
public static class OutputCachingExtensions
{
    public const string UsersListPolicy = "users-list";
    public const string UsersTag = "users";

    // The safety net behind tag eviction, not the primary invalidation. Kept short because a stale list is
    // visible to every caller.
    private static readonly TimeSpan UsersListExpiry = TimeSpan.FromMinutes(5);

    public static IServiceCollection AddApiOutputCaching(this IServiceCollection services)
    {
        services.AddOutputCache(options => options.AddPolicy(
            UsersListPolicy,
            policy => policy
                .Tag(UsersTag)
                .Expire(UsersListExpiry)
                .AddPolicy<CacheAuthenticatedRequestsPolicy>()));

        return services;
    }
}
