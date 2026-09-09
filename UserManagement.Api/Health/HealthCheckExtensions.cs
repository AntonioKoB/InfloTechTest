using Microsoft.Extensions.DependencyInjection;
using UserManagement.Data;

namespace UserManagement.Api.Health;

public static class HealthCheckExtensions
{
    public const string DatabaseCheckName = "database";

    /// <summary>
    /// A probe endpoint for whatever hosts the API (App Service's Health check, a load balancer, a container
    /// orchestrator): reports Healthy only while the database answers, so a process that is up but cut off
    /// from its data is reported as down. Mapped in Program.cs as an anonymous endpoint - probes carry no
    /// token - and it exposes nothing beyond "up" or "down".
    /// </summary>
    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks().AddDbContextCheck<DataContext>(DatabaseCheckName);
        return services;
    }
}
