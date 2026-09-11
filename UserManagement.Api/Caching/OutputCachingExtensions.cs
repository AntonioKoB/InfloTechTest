using System;
using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Api.Caching;

/// <summary>
/// Output caching for the users list: one entry per filter value, tagged so a completed write can evict them
/// all, time-boxed as a safety net. The single user is cached in the service layer instead, beneath the
/// audit.
/// </summary>
public static class OutputCachingExtensions
{
    public const string UsersListPolicy = "users-list";
    public const string UsersTag = "users";

    // Safety net behind tag eviction.
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
