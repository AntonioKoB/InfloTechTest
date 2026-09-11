using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Blazor.Telemetry;

public static class TelemetryExtensions
{
    public const string ConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>
    /// Application Insights for this host, registered only when a connection string is configured: the SDK
    /// throws at startup without one, and a local run has none.
    /// </summary>
    public static IServiceCollection AddBlazorTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration[ConnectionStringKey]))
        {
            services.AddApplicationInsightsTelemetry(configuration);
        }

        return services;
    }
}
