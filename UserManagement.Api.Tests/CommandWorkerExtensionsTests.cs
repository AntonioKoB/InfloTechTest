using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UserManagement.Api.Commands;

namespace UserManagement.Api.Tests;

/// <summary>
/// Locks in that the worker is hosted by the API: without this registration commands are accepted and never
/// executed, and nothing else in the suite would notice.
/// </summary>
public class CommandWorkerExtensionsTests
{
    [Fact]
    public void AddCommandWorker_WhenCalled_MustRegisterTheWorkerAsAHostedService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCommandWorker();

        // Assert
        services.Should().ContainSingle(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(CommandWorker));
    }
}
