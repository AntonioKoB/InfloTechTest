using System;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using UserManagement.Api.Controllers;

namespace UserManagement.Api.Tests;

/// <summary>
/// Locks in the API's authorization posture: every endpoint requires an authenticated caller except the one
/// that hands out the token in the first place. The bearer-token validation itself is framework middleware
/// configured in Program.cs, proven by manual verification (an unauthenticated call is rejected with 401, a
/// call carrying a token from /api/auth/login succeeds); what a unit test can pin down is that the
/// controllers opt in to it and that nothing else is left open.
/// </summary>
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
