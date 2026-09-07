using UserManagement.Data;
using UserManagement.Services.Domain.Implementations;
using UserManagement.Services.Domain.Interfaces;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
        => services
            .AddScoped<UserService>()
            .AddScoped<IUserLogService, UserLogService>()
            .AddScoped<IUserService>(sp => new AuditingUserService(
                sp.GetRequiredService<UserService>(),
                sp.GetRequiredService<IUserLogService>(),
                sp.GetRequiredService<IDataContext>()));
}
