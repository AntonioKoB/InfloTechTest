using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Api.Telemetry;

namespace UserManagement.Api.Tests;

/// <summary>
/// Locks in the API's telemetry registration: Application Insights is wired only when a connection string is
/// configured, so a local run without one starts exactly as before and sends nothing. What the SDK collects
/// once registered (requests, SQL dependencies, exceptions, logs) is the SDK's contract, proven against the
/// deployed environment rather than here.
/// </summary>
public class TelemetryExtensionsTests
{
    private const string ConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    // A syntactically valid connection string that is never contacted: no telemetry pipeline is started here.
    private const string ConnectionString =
        "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://example.invalid/";

    [Fact]
    public void AddApiTelemetry_WhenConnectionStringConfigured_MustRegisterTelemetryClientWithIt()
    {
        // Arrange
        var configuration = BuildConfiguration(new Dictionary<string, string?> { [ConnectionStringKey] = ConnectionString });
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddApiTelemetry(configuration);
        using var provider = services.BuildServiceProvider();

        // Act
        var client = provider.GetService<TelemetryClient>();
        var telemetryConfiguration = provider.GetRequiredService<TelemetryConfiguration>();

        // Assert
        client.Should().NotBeNull();
        telemetryConfiguration.ConnectionString.Should().Be(ConnectionString);
    }

    [Fact]
    public void AddApiTelemetry_WhenConnectionStringAbsent_MustRegisterNothing()
    {
        // Arrange
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddApiTelemetry(configuration);
        using var provider = services.BuildServiceProvider();

        // Act
        var client = provider.GetService<TelemetryClient>();

        // Assert
        client.Should().BeNull();
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
