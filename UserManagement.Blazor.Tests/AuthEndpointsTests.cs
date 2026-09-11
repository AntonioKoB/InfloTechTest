using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;
using Refit;
using UserManagement.Api.Contracts.Auth;
using UserManagement.Blazor.Api;
using UserManagement.Blazor.Auth;

namespace UserManagement.Blazor.Tests;

public class AuthEndpointsTests
{
    [Fact]
    public async Task LoginAsync_WhenTheApiAcceptsTheCredentials_MustSignInWithTheTokenAndRedirectHome()
    {
        // Arrange
        var response = SetupSuccessfulLogin();

        // Act
        var result = await AuthEndpoints.LoginAsync(CreateLoginHttpContext(), _authApi.Object, LoggerFactory);

        // Assert
        result.Should().BeOfType<RedirectHttpResult>().Which.Url.Should().Be("/");
        _authenticationService.Verify(s => s.SignInAsync(
            It.IsAny<HttpContext>(),
            CookieAuthenticationDefaults.AuthenticationScheme,
            It.Is<ClaimsPrincipal>(p => p.FindFirst(AuthClaimTypes.AccessToken)!.Value == response.Token && p.Identity!.Name == response.DisplayName),
            It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_MustExpireTheCookieWhenTheTokenExpires()
    {
        // Arrange
        var response = SetupSuccessfulLogin();
        AuthenticationProperties? properties = null;
        _authenticationService
            .Setup(s => s.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Callback<HttpContext, string?, ClaimsPrincipal, AuthenticationProperties?>((_, _, _, p) => properties = p)
            .Returns(Task.CompletedTask);

        // Act
        await AuthEndpoints.LoginAsync(CreateLoginHttpContext(), _authApi.Object, LoggerFactory);

        // Assert
        properties.Should().NotBeNull();
        properties!.ExpiresUtc.Should().Be(response.ExpiresAtUtc);
    }

    [Theory]
    [InlineData("/users", "/users")]
    [InlineData("/logs?page=2", "/logs?page=2")]
    [InlineData("https://evil.example/", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("", "/")]
    public async Task LoginAsync_MustFollowTheReturnUrlOnlyWhenItStaysWithinThisSite(string returnUrl, string expectedRedirect)
    {
        // Arrange
        SetupSuccessfulLogin();

        // Act
        var result = await AuthEndpoints.LoginAsync(CreateLoginHttpContext(returnUrl: returnUrl), _authApi.Object, LoggerFactory);

        // Assert
        result.Should().BeOfType<RedirectHttpResult>().Which.Url.Should().Be(expectedRedirect);
    }

    [Fact]
    public async Task LoginAsync_WhenApiRejectsCredentials_MustRedirectToLoginWithErrorWithoutSigningIn()
    {
        // Arrange
        _authApi.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>())).ThrowsAsync(await CreateUnauthorizedException());

        // Act
        var result = await AuthEndpoints.LoginAsync(CreateLoginHttpContext(), _authApi.Object, LoggerFactory);

        // Assert
        result.Should().BeOfType<RedirectHttpResult>().Which.Url.Should().Be("/login?error=1");
        _authenticationService.Verify(s => s.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenAntiforgeryNotValidated_MustReturnBadRequestWithoutReadingForm()
    {
        // Arrange
        SetupSuccessfulLogin();

        // Act
        var result = await AuthEndpoints.LoginAsync(CreateLoginHttpContext(antiforgeryValidationPassed: false, poisonForm: true), _authApi.Object, LoggerFactory);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeHttpResult>().Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        _authApi.Verify(a => a.LoginAsync(It.IsAny<LoginRequest>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_MustEndApiSessionWithCookieTokenThenSignOutAndRedirect()
    {
        // Arrange

        // Act
        var result = await AuthEndpoints.LogoutAsync(CreateHttpContext(sessionToken: "jwt-token"), _authApi.Object, LoggerFactory);

        // Assert
        _authApi.Verify(a => a.LogoutAsync("jwt-token"), Times.Once);
        _authenticationService.Verify(s => s.SignOutAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<AuthenticationProperties?>()), Times.Once);
        result.Should().BeOfType<RedirectHttpResult>().Which.Url.Should().Be("/login");
    }

    [Fact]
    public async Task LogoutAsync_WhenTheApiNoLongerAcceptsTheToken_MustStillSignOutLocally()
    {
        // Arrange
        _authApi.Setup(a => a.LogoutAsync(It.IsAny<string>())).ThrowsAsync(await CreateUnauthorizedException());

        // Act
        var result = await AuthEndpoints.LogoutAsync(CreateHttpContext(sessionToken: "expired-token"), _authApi.Object, LoggerFactory);

        // Assert
        _authenticationService.Verify(s => s.SignOutAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<AuthenticationProperties?>()), Times.Once);
        result.Should().BeOfType<RedirectHttpResult>().Which.Url.Should().Be("/login");
    }

    [Fact]
    public async Task LogoutAsync_WhenAntiforgeryNotValidated_MustReturnBadRequestWithoutSigningOut()
    {
        // Arrange

        // Act
        var result = await AuthEndpoints.LogoutAsync(CreateHttpContext(antiforgeryValidationPassed: false, sessionToken: "jwt-token"), _authApi.Object, LoggerFactory);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeHttpResult>().Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        _authApi.Verify(a => a.LogoutAsync(It.IsAny<string>()), Times.Never);
        _authenticationService.Verify(s => s.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties?>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenApiRejectsCredentials_MustLogEmailAndStatusNotPassword()
    {
        // Arrange
        _authApi.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>())).ThrowsAsync(await CreateUnauthorizedException());

        // Act
        await AuthEndpoints.LoginAsync(CreateLoginHttpContext(), _authApi.Object, LoggerFactory);

        // Assert
        var record = LogRecords.Should().ContainSingle().Which;
        record.Level.Should().Be(LogLevel.Warning);
        record.Message.Should().Contain("ploew@example.com").And.Contain("Unauthorized").And.NotContain("12345");
    }

    [Fact]
    public async Task LogoutAsync_WhenTheApiNoLongerAcceptsTheToken_MustLogTheStatusAtWarning()
    {
        // Arrange
        _authApi.Setup(a => a.LogoutAsync(It.IsAny<string>())).ThrowsAsync(await CreateUnauthorizedException());

        // Act
        await AuthEndpoints.LogoutAsync(CreateHttpContext(sessionToken: "expired-token"), _authApi.Object, LoggerFactory);

        // Assert
        var record = LogRecords.Should().ContainSingle().Which;
        record.Level.Should().Be(LogLevel.Warning);
        record.Message.Should().Contain("Unauthorized").And.NotContain("expired-token");
    }

    [Fact]
    public async Task LogoutAsync_WhenTheAntiforgeryTokenWasNotValidated_MustLogAtWarning()
    {
        // Arrange

        // Act
        await AuthEndpoints.LogoutAsync(CreateHttpContext(antiforgeryValidationPassed: false, sessionToken: "jwt-token"), _authApi.Object, LoggerFactory);

        // Assert
        LogRecords.Should().ContainSingle().Which.Level.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public async Task MapAuthEndpoints_LoginPost_MustDeclareTheFormContentTypesItAccepts()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(_authApi.Object);
        await using var app = builder.Build();

        // Act
        app.MapAuthEndpoints();
        var login = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == "/login");

        // Assert
        var accepts = login.Metadata.GetMetadata<IAcceptsMetadata>();
        accepts.Should().NotBeNull();
        accepts!.ContentTypes.Should().Contain("application/x-www-form-urlencoded");
    }

    [Theory]
    [InlineData(nameof(AuthEndpoints.LoginAsync))]
    [InlineData(nameof(AuthEndpoints.LogoutAsync))]
    public void FormPostHandlers_MustRequireAnAntiforgeryToken(string handlerName)
    {
        // Arrange
        var handler = typeof(AuthEndpoints).GetMethod(handlerName)!;

        // Act
        var metadata = handler.GetCustomAttribute<RequireAntiforgeryTokenAttribute>();

        // Assert
        metadata.Should().NotBeNull($"{handlerName} must validate the antiforgery token");
        metadata!.RequiresValidation.Should().BeTrue();
    }

    private LoginResponse SetupSuccessfulLogin()
    {
        var response = new LoginResponse
        {
            Token = "jwt-token",
            ExpiresAtUtc = new DateTime(2026, 9, 9, 18, 0, 0, DateTimeKind.Utc),
            DisplayName = "Peter Loew",
            Email = "ploew@example.com"
        };

        _authApi.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>())).ReturnsAsync(response);

        return response;
    }

    private HttpContext CreateHttpContext(bool antiforgeryValidationPassed = true, string? sessionToken = null)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(_authenticationService.Object).BuildServiceProvider()
        };

        httpContext.Features.Set<IAntiforgeryValidationFeature>(new StubAntiforgeryValidationFeature(antiforgeryValidationPassed));

        if (sessionToken is not null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "Peter Loew"), new Claim(AuthClaimTypes.AccessToken, sessionToken)],
                CookieAuthenticationDefaults.AuthenticationScheme));
        }

        return httpContext;
    }

    private HttpContext CreateLoginHttpContext(bool antiforgeryValidationPassed = true, string? returnUrl = null, bool poisonForm = false)
    {
        var httpContext = CreateHttpContext(antiforgeryValidationPassed);

        if (poisonForm)
        {
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            httpContext.Request.Body = new ThrowingStream();
        }
        else
        {
            var fields = new Dictionary<string, StringValues>
            {
                ["Email"] = "ploew@example.com",
                ["Password"] = "12345"
            };
            if (returnUrl is not null)
                fields["ReturnUrl"] = returnUrl;

            httpContext.Request.Form = new FormCollection(fields);
        }

        return httpContext;
    }

    private sealed class ThrowingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("The form must not be read before antiforgery validation is checked.");
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new InvalidOperationException("The form must not be read before antiforgery validation is checked.");
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private static async Task<ApiException> CreateUnauthorizedException()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost/api/auth/login");
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("", Encoding.UTF8, "application/json") };
        return await ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    private readonly Mock<IAuthApi> _authApi = new();
    private readonly Mock<IAuthenticationService> _authenticationService = new();

    private readonly ServiceProvider _logging = new ServiceCollection().AddLogging(b => b.AddFakeLogging()).BuildServiceProvider();
    private ILoggerFactory LoggerFactory => _logging.GetRequiredService<ILoggerFactory>();
    private IReadOnlyList<FakeLogRecord> LogRecords => _logging.GetFakeLogCollector().GetSnapshot();

    private sealed class StubAntiforgeryValidationFeature(bool isValid) : IAntiforgeryValidationFeature
    {
        public bool IsValid => isValid;
        public Exception? Error => null;
    }
}
