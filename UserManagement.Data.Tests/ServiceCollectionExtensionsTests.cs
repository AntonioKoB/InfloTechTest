using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Data.Tests;

/// <summary>
/// Locks in the data-access registration's connection resiliency: the SQL Server provider must be configured
/// with a retrying execution strategy and the budget chosen for it. Building the context here never opens a
/// connection - the strategy is decided from the options alone - so any well-formed connection string will do.
/// The retry behaviour itself is provider code, proven by manual verification against a stopped instance.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDataAccess_WhenCalled_MustConfigureARetryingExecutionStrategy()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=InfloUsersDB;Trusted_Connection=True;TrustServerCertificate=True;"
            })
            .Build();
        using var provider = new ServiceCollection().AddDataAccess(configuration).BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        // Act: Invokes the method under test with the arranged parameters.
        var strategy = context.Database.CreateExecutionStrategy();

        // Assert: Verifies that the action of the method under test behaves as expected.
        var retrying = strategy.Should().BeOfType<SqlServerRetryingExecutionStrategy>().Subject;
        retrying.MaxRetryCount.Should().Be(3);
        retrying.MaxRetryDelay.Should().Be(TimeSpan.FromSeconds(5));
    }
}
