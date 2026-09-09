using Microsoft.AspNetCore.Identity;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Domain.Implementations;
using UserManagement.Services.Domain.Interfaces;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
        => services
            .AddScoped<UserService>()
            .AddScoped<IUserLogService, UserLogService>()
            .AddScoped<IUserLogDiffBuilder, UserLogDiffBuilder>()
            .AddScoped<IUserService>(sp => new AuditingUserService(
                sp.GetRequiredService<UserService>(),
                sp.GetRequiredService<IUserLogService>(),
                sp.GetRequiredService<IDataContext>()))
            .AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>()
            .AddScoped<CredentialService>()
            .AddScoped<ICredentialService>(sp => new AuditingCredentialService(
                sp.GetRequiredService<CredentialService>(),
                sp.GetRequiredService<IUserLogService>()));
}
