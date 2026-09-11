using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Blazor.Telemetry;

public static class TelemetryExtensions
{
    public const string ConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>
    /// Application Insights for this host: page requests, the HTTP calls to the API (which carry W3C trace
    /// context, so the API's own telemetry joins the same transaction), unhandled exceptions and logger
    /// output. Registered only when a connection string is configured: the SDK refuses to start without one,
    /// and a local run has none, so the guard is what keeps local startup unchanged and silent.
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
