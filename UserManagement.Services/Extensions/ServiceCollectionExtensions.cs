using Microsoft.AspNetCore.Identity;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Caching;
using UserManagement.Services.Commands;
using UserManagement.Services.Domain.Implementations;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Services.Messaging;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
        => services
            .AddMemoryCache()
            .AddSingleton<ICache, MemoryCacheAdapter>()
            .AddSingleton<IMessageBus, InMemoryMessageBus>()
            .AddSingleton<ICommandStatusStore, InMemoryCommandStatusStore>()
            .AddScoped<UserService>()
            .AddScoped<IUserLogService, UserLogService>()
            .AddScoped<IUserLogDiffBuilder, UserLogDiffBuilder>()
            // Auditing wraps caching on purpose: a read served from the cache is still recorded as a view.
            .AddScoped<IUserService>(sp => new AuditingUserService(
                new CachingUserService(sp.GetRequiredService<UserService>(), sp.GetRequiredService<ICache>()),
                sp.GetRequiredService<IUserLogService>(),
                sp.GetRequiredService<IDataContext>()))
            .AddScoped<ICommandHandler<CreateUserCommand>, CreateUserCommandHandler>()
            .AddScoped<ICommandHandler<UpdateUserCommand>, UpdateUserCommandHandler>()
            .AddScoped<ICommandHandler<DeleteUserCommand>, DeleteUserCommandHandler>()
            .AddScoped<ICommandHandler<RecordUserLogCommand>, RecordUserLogCommandHandler>()
            .AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>()
            .AddScoped<CredentialService>()
            .AddScoped<ICredentialService>(sp => new AuditingCredentialService(
                sp.GetRequiredService<CredentialService>(),
                sp.GetRequiredService<IUserLogService>()));
}
