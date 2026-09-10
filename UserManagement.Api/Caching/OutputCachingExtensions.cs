using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Api.Caching;

public static class OutputCachingExtensions
{
    public const string UsersListPolicy = "users-list";
    public const string UsersTag = "users";

    public static IServiceCollection AddApiOutputCaching(this IServiceCollection services)
    {
        services.AddOutputCache(options => options.AddPolicy(
            UsersListPolicy,
            policy => policy.Tag(UsersTag).AddPolicy<CacheAuthenticatedRequestsPolicy>()));

        return services;
    }
}
