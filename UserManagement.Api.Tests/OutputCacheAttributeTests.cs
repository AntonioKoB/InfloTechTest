using System.Reflection;
using Microsoft.AspNetCore.OutputCaching;
using UserManagement.Api.Caching;
using UserManagement.Api.Controllers;

namespace UserManagement.Api.Tests;

/// <summary>
/// Locks in what the API caches: the users list and nothing else. The output-cache middleware, the named
/// policy it resolves and the actual cache hits and evictions over HTTP are framework behaviour configured
/// in Program.cs, proven by manual verification (a repeated GET is served from cache; a write makes the next
/// GET fresh); what a unit test can pin down is which actions opt in.
/// </summary>
public class OutputCacheAttributeTests
{
    [Fact]
    public void GetUsers_MustBeOutputCachedUnderTheUsersListPolicy()
    {
        // Act
        var attribute = typeof(UsersController).GetMethod(nameof(UsersController.GetUsers))!.GetCustomAttribute<OutputCacheAttribute>();

        // Assert
        attribute.Should().NotBeNull("the users list is the one response worth caching");
        attribute!.PolicyName.Should().Be(OutputCachingExtensions.UsersListPolicy);
    }

    [Theory]
    [InlineData(nameof(UsersController.GetById))]
    [InlineData(nameof(UsersController.GetUserLogs))]
    [InlineData(nameof(UsersController.Create))]
    [InlineData(nameof(UsersController.Update))]
    [InlineData(nameof(UsersController.Delete))]
    public void OtherUsersEndpoints_MustNotBeOutputCached(string actionName)
    {
        // Act
        var attribute = typeof(UsersController).GetMethod(actionName)!.GetCustomAttribute<OutputCacheAttribute>();

        // Assert
        attribute.Should().BeNull($"{actionName} must always hit the service: single users feed the audit trail and writes must never be cached");
    }
}
