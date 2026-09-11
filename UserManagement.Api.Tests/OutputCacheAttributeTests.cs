using System.Reflection;
using Microsoft.AspNetCore.OutputCaching;
using UserManagement.Api.Caching;
using UserManagement.Api.Controllers;

namespace UserManagement.Api.Tests;

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
