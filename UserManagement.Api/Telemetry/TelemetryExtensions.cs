using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Api.Telemetry;

public static class TelemetryExtensions
{
    public const string ConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>
    /// Application Insights for this host: requests, the SQL calls behind them, unhandled exceptions and
    /// logger output, correlated with the caller's request by W3C trace context. Registered only when a
    /// connection string is configured: the SDK refuses to start without one, and a local run has none, so
    /// the guard is what keeps local startup unchanged and silent.
    /// </summary>
    public static IServiceCollection AddApiTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration[ConnectionStringKey]))
        {
            services.AddApplicationInsightsTelemetry(configuration);
        }

        return services;
    }
}
