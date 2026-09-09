using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using UserManagement.Api.Health;

namespace UserManagement.Api.Tests;

/// <summary>
/// Locks in the API's health-check registration: the database connectivity check is what makes /health mean
/// "up and able to serve" rather than "the process exists". The endpoint mapping, its anonymous access and
/// the 200/503 responses are framework middleware configured in Program.cs, proven by manual verification.
/// </summary>
public class HealthCheckExtensionsTests
{
    [Fact]
    public void AddApiHealthChecks_WhenCalled_MustRegisterTheDatabaseCheck()
    {
        // Arrange
        var services = new ServiceCollection().AddApiHealthChecks();
        using var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        // Assert
        options.Registrations.Should().ContainSingle(r => r.Name == HealthCheckExtensions.DatabaseCheckName);
    }
}
