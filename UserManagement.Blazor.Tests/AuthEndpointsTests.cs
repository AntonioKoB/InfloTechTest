using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
        // The cookie is only useful while the token inside it is accepted by the API, so the two lifetimes
        // are tied together rather than letting the cookie outlive a token the API will reject.
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
        // The cookie middleware sends users to /login?ReturnUrl=<where they were heading>; honouring it is a
        // nicety, but an absolute or protocol-relative value could bounce a freshly signed-in user anywhere.
        SetupSuccessfulLogin();

        // Act
        var result = await AuthEndpoints.LoginAsync(CreateLoginHttpContext(returnUrl: returnUrl), _authApi.Object, LoggerFactory);

        // Assert
        result.Should().BeOfType<RedirectHttpResult>().Which.Url.Should().Be(expectedRedirect);
    }

    [Fact]
    public async Task LoginAsync_WhenTheApiRejectsTheCredentials_MustRedirectBackToLoginWithAnErrorWithoutSigningIn()
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
    public async Task LoginAsync_WhenTheAntiforgeryTokenWasNotValidated_MustReturnBadRequestWithoutReadingTheFormOrCallingTheApi()
    {
        // Arrange
        // The real bug this guards against: ASP.NET Core throws if the form is read while antiforgery
        // validation is invalid, and [FromForm] binding used to read it before this handler's own check
        // ever ran - turning every stale-token login into an unhandled 500 instead of this 400. The request
        // body here throws if touched, so the handler reading the form before checking would fail this test
        // with that exception rather than quietly returning 400 for the wrong reason.
        SetupSuccessfulLogin();

        // Act
        var result = await AuthEndpoints.LoginAsync(CreateLoginHttpContext(antiforgeryValidationPassed: false, poisonForm: true), _authApi.Object, LoggerFactory);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeHttpResult>().Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        _authApi.Verify(a => a.LoginAsync(It.IsAny<LoginRequest>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_MustEndTheSessionOnTheApiWithTheCookieTokenThenSignOutAndRedirectToLogin()
    {
        // Arrange
        // The API records the sign-out against the session; it needs the token that identifies it, which
        // only the cookie principal holds.

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
        // An expired token is the normal reason for this; the cookie is what keeps the browser signed in and
        // must be cleared regardless.
        _authApi.Setup(a => a.LogoutAsync(It.IsAny<string>())).ThrowsAsync(await CreateUnauthorizedException());

        // Act
        var result = await AuthEndpoints.LogoutAsync(CreateHttpContext(sessionToken: "expired-token"), _authApi.Object, LoggerFactory);

        // Assert
        _authenticationService.Verify(s => s.SignOutAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, It.IsAny<AuthenticationProperties?>()), Times.Once);
        result.Should().BeOfType<RedirectHttpResult>().Which.Url.Should().Be("/login");
    }

    [Fact]
    public async Task LogoutAsync_WhenTheAntiforgeryTokenWasNotValidated_MustReturnBadRequestWithoutSigningOut()
    {
        // Arrange
        // The attribute alone is not enough for a handler that binds no form parameters: the middleware
        // records the validation outcome but nothing turns a failure into a rejection, so a forged or
        // token-less post would still sign the user out. The handler has to check the outcome itself.

        // Act
        var result = await AuthEndpoints.LogoutAsync(CreateHttpContext(antiforgeryValidationPassed: false, sessionToken: "jwt-token"), _authApi.Object, LoggerFactory);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeHttpResult>().Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        _authApi.Verify(a => a.LogoutAsync(It.IsAny<string>()), Times.Never);
        _authenticationService.Verify(s => s.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties?>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenTheApiRejectsTheCredentials_MustLogTheEmailAndStatusAtWarningWithoutThePassword()
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
        // The sign-out still succeeds locally, so this is the only trace that the API call failed.
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
        // A rejected post is either a forged request or a broken page; both deserve a trace.

        // Act
        await AuthEndpoints.LogoutAsync(CreateHttpContext(antiforgeryValidationPassed: false, sessionToken: "jwt-token"), _authApi.Object, LoggerFactory);

        // Assert
        LogRecords.Should().ContainSingle().Which.Level.Should().Be(LogLevel.Warning);
    }

    [Theory]
    [InlineData(nameof(AuthEndpoints.LoginAsync))]
    [InlineData(nameof(AuthEndpoints.LogoutAsync))]
    public void FormPostHandlers_MustRequireAnAntiforgeryToken(string handlerName)
    {
        // Arrange
        // Both handlers change who the browser is signed in as, and both are plain form posts a third-party
        // page could forge. Requiring the antiforgery token (issued to this app's own pages) closes that.
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

        // What the antiforgery middleware leaves behind after checking the token on a real request.
        httpContext.Features.Set<IAntiforgeryValidationFeature>(new StubAntiforgeryValidationFeature(antiforgeryValidationPassed));

        if (sessionToken is not null)
        {
            // What the cookie middleware leaves behind for a signed-in browser.
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "Peter Loew"), new Claim(AuthClaimTypes.AccessToken, sessionToken)],
                CookieAuthenticationDefaults.AuthenticationScheme));
        }

        return httpContext;
    }

    // LoginAsync reads Email/Password/ReturnUrl from the form itself (not [FromForm] binding - see
    // AuthEndpoints for why), so its HttpContext needs a form. poisonForm gives it a body that throws if
    // read instead, for the one test proving the handler never reads the form before checking antiforgery
    // validity.
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

    // Fails the test loudly if anything tries to read the request body, instead of letting a premature
    // form read succeed quietly - proving the ordering the real bug depended on getting wrong.
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

    // The handlers live on a static class, so they take the factory rather than an ILogger<T>; the fake
    // provider behind it collects whatever they log.
    private readonly ServiceProvider _logging = new ServiceCollection().AddLogging(b => b.AddFakeLogging()).BuildServiceProvider();
    private ILoggerFactory LoggerFactory => _logging.GetRequiredService<ILoggerFactory>();
    private IReadOnlyList<FakeLogRecord> LogRecords => _logging.GetFakeLogCollector().GetSnapshot();

    // The framework's own implementation is internal; the interface is all the handlers depend on.
    private sealed class StubAntiforgeryValidationFeature(bool isValid) : IAntiforgeryValidationFeature
    {
        public bool IsValid => isValid;
        public Exception? Error => null;
    }
}
