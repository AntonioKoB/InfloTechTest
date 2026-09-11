using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using UserManagement.Api.Auth;
using UserManagement.Api.Contracts.Auth;
using UserManagement.Api.Controllers;
using UserManagement.Models;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Api.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_MustPassTheSubmittedEmailAndPasswordToTheCredentialService()
    {
        // Arrange
        var controller = CreateController();
        SetupAuthenticatedUser();

        // Act
        await controller.Login(new LoginRequest { Email = "juser@example.com", Password = "12345" });

        // Assert
        _credentialService.Verify(s => s.AuthenticateAsync("juser@example.com", "12345"), Times.Once);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_MustReturnOkWithTheIssuedToken()
    {
        // Arrange
        var controller = CreateController();
        var user = SetupAuthenticatedUser();
        var issued = new IssuedToken("signed.jwt.token", new DateTime(2026, 9, 9, 18, 0, 0, DateTimeKind.Utc));
        _jwtTokenService.Setup(s => s.CreateToken(user)).Returns(issued);

        // Act
        var result = await controller.Login(new LoginRequest { Email = user.Email, Password = "12345" });

        // Assert
        var response = result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<LoginResponse>().Subject;
        response.Token.Should().Be(issued.Value);
        response.ExpiresAtUtc.Should().Be(issued.ExpiresAtUtc);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_MustDescribeTheSignedInUser()
    {
        // Arrange
        var controller = CreateController();
        var user = SetupAuthenticatedUser();
        _jwtTokenService.Setup(s => s.CreateToken(user)).Returns(new IssuedToken("signed.jwt.token", DateTime.UtcNow.AddHours(1)));

        // Act
        var result = await controller.Login(new LoginRequest { Email = user.Email, Password = "12345" });

        // Assert
        var response = result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<LoginResponse>().Subject;
        response.DisplayName.Should().Be("Johnny User");
        response.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_MustReturnUnauthorizedWithoutIssuingAToken()
    {
        // Arrange
        // A single 401 for every failure reason (unknown email, wrong password, inactive user) - the
        // credential service already collapses them to null, and telling a caller which one it was would
        // leak whether an email is registered.
        var controller = CreateController();
        _credentialService.Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act
        var result = await controller.Login(new LoginRequest { Email = "nobody@example.com", Password = "wrong" });

        // Assert
        result.Result.Should().BeAssignableTo<IStatusCodeActionResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _jwtTokenService.Verify(s => s.CreateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_MustLogTheEmailAtWarningWithoutThePassword()
    {
        // Arrange
        // A run of these for one email is what a guessing attempt looks like, so it is a Warning. The
        // submitted password must never reach a log.
        var controller = CreateController();
        _credentialService.Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act
        await controller.Login(new LoginRequest { Email = "nobody@example.com", Password = "wrong-secret" });

        // Assert
        var record = _logger.Collector.GetSnapshot().Should().ContainSingle().Which;
        record.Level.Should().Be(LogLevel.Warning);
        record.Message.Should().Contain("nobody@example.com").And.NotContain("wrong-secret");
    }

    [Fact]
    public async Task Logout_MustEndTheSessionOfTheUserNamedInTheBearerToken()
    {
        // Arrange
        // No body on this endpoint: the validated token's subject is the only trustworthy statement of who is
        // signing out.
        var controller = CreateController();
        SignInAs(controller, userId: 7);

        // Act
        var result = await controller.Logout();

        // Assert
        _credentialService.Verify(s => s.SignOutAsync(7), Times.Once);
        result.Should().BeOfType<NoContentResult>();
    }

    private static void SignInAs(AuthController controller, long userId)
        => controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())], "Bearer"))
            }
        };

    private User SetupAuthenticatedUser()
    {
        var user = new User
        {
            Id = 7,
            Forename = "Johnny",
            Surname = "User",
            Email = "juser@example.com",
            IsActive = true,
            DateOfBirth = new DateOnly(1990, 1, 1)
        };

        _credentialService.Setup(s => s.AuthenticateAsync(user.Email, It.IsAny<string>())).ReturnsAsync(user);
        _jwtTokenService.Setup(s => s.CreateToken(user)).Returns(new IssuedToken("signed.jwt.token", DateTime.UtcNow.AddHours(1)));

        return user;
    }

    private readonly Mock<ICredentialService> _credentialService = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly FakeLogger<AuthController> _logger = new();
    private AuthController CreateController() => new(_credentialService.Object, _jwtTokenService.Object, _logger);
}
