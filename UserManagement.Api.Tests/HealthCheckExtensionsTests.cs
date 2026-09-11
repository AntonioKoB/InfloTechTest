using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using UserManagement.Api.Health;

namespace UserManagement.Api.Tests;

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
