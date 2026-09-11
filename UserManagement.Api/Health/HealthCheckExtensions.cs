using Microsoft.Extensions.DependencyInjection;
using UserManagement.Data;

namespace UserManagement.Api.Health;

public static class HealthCheckExtensions
{
    public const string DatabaseCheckName = "database";

    /// <summary>
    /// Health probe: Healthy only while the database answers. Mapped anonymously in Program.cs; probes carry
    /// no token.
    /// </summary>
    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks().AddDbContextCheck<DataContext>(DatabaseCheckName);
        return services;
    }
}
