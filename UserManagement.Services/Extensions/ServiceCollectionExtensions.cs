using Microsoft.AspNetCore.Identity;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Caching;
using UserManagement.Services.Domain.Implementations;
using UserManagement.Services.Domain.Interfaces;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
        => services
            .AddMemoryCache()
            .AddSingleton<ICache, MemoryCacheAdapter>()
            .AddScoped<UserService>()
            .AddScoped<IUserLogService, UserLogService>()
            .AddScoped<IUserLogDiffBuilder, UserLogDiffBuilder>()
            .AddScoped<IUserService>(sp => new AuditingUserService(
                new CachingUserService(sp.GetRequiredService<UserService>(), sp.GetRequiredService<ICache>()),
                sp.GetRequiredService<IUserLogService>(),
                sp.GetRequiredService<IDataContext>()))
            .AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>()
            .AddScoped<CredentialService>()
            .AddScoped<ICredentialService>(sp => new AuditingCredentialService(
                sp.GetRequiredService<CredentialService>(),
                sp.GetRequiredService<IUserLogService>()));
}
