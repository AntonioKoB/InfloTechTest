using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UserManagement.Api.Commands;

namespace UserManagement.Api.Tests;

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
