using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserManagement.Blazor.Telemetry;

namespace UserManagement.Blazor.Tests;

public class TelemetryExtensionsTests
{
    private const string ConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    private const string ConnectionString =
        "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://example.invalid/";

    [Fact]
    public void AddBlazorTelemetry_WhenConnectionStringConfigured_MustRegisterTelemetryClient()
    {
        // Arrange
        var configuration = BuildConfiguration(new Dictionary<string, string?> { [ConnectionStringKey] = ConnectionString });
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddBlazorTelemetry(configuration);
        using var provider = services.BuildServiceProvider();

        // Act
        var client = provider.GetService<TelemetryClient>();
        var telemetryConfiguration = provider.GetRequiredService<TelemetryConfiguration>();

        // Assert
        client.Should().NotBeNull();
        telemetryConfiguration.ConnectionString.Should().Be(ConnectionString);
    }

    [Fact]
    public void AddBlazorTelemetry_WhenConnectionStringAbsent_MustRegisterNothing()
    {
        // Arrange
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddBlazorTelemetry(configuration);
        using var provider = services.BuildServiceProvider();

        // Act
        var client = provider.GetService<TelemetryClient>();

        // Assert
        client.Should().BeNull();
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
