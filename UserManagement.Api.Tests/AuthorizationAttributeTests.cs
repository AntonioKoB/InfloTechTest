using System;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using UserManagement.Api.Controllers;

namespace UserManagement.Api.Tests;

public class AuthorizationAttributeTests
{
    [Theory]
    [InlineData(typeof(UsersController))]
    [InlineData(typeof(LogsController))]
    [InlineData(typeof(AuthController))]
    public void ProtectedControllers_MustRequireAnAuthenticatedCaller(Type controllerType)
    {
        // Act
        var attribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        attribute.Should().NotBeNull($"{controllerType.Name} must carry [Authorize] so every action on it requires a bearer token");
    }

    [Fact]
    public void AuthController_Login_MustAllowAnonymousCallers()
    {
        // Act
        var attribute = typeof(AuthController).GetMethod(nameof(AuthController.Login))!.GetCustomAttribute<AllowAnonymousAttribute>();

        // Assert
        attribute.Should().NotBeNull("login is how a caller obtains a token, so it cannot itself require one");
    }
}
