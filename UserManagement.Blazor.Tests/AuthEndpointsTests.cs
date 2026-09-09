using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
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
        var result = await AuthEndpoints.LoginAsync(CreateRequest(), returnUrl: null, _authApi.Object, CreateHttpContext());

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
        await AuthEndpoints.LoginAsync(CreateRequest(), returnUrl: null, _authApi.Object, CreateHttpContext());

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
        var result = await AuthEndpoints.LoginAsync(CreateRequest(), returnUrl, _authApi.Object, CreateHttpContext());

        // Assert
        result.Should().BeOfType<RedirectHttpResult>().Which.Url.Should().Be(expectedRedirect);
    }

    [Fact]
    public async Task LoginAsync_WhenTheApiRejectsTheCredentials_MustRedirectBackToLoginWithAnErrorWithoutSigningIn()
    {
        // Arrange
        _authApi.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>())).ThrowsAsync(await CreateUnauthorizedException());

        // Act
        var result = await AuthEndpoints.LoginAsync(CreateRequest(), returnUrl: null, _authApi.Object, CreateHttpContext());

        // Assert
        result.Should().BeOfType<RedirectHttpResult>().Which.Url.Should().Be("/login?error=1");
        _authenticationService.Verify(s => s.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenTheAntiforgeryTokenWasNotValidated_MustReturnBadRequestWithoutCallingTheApi()
    {
        // Arrange
        SetupSuccessfulLogin();

        // Act
        var result = await AuthEndpoints.LoginAsync(CreateRequest(), returnUrl: null, _authApi.Object, CreateHttpContext(antiforgeryValidationPassed: false));

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
        var result = await AuthEndpoints.LogoutAsync(CreateHttpContext(sessionToken: "jwt-token"), _authApi.Object);

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
        var result = await AuthEndpoints.LogoutAsync(CreateHttpContext(sessionToken: "expired-token"), _authApi.Object);

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
        var result = await AuthEndpoints.LogoutAsync(CreateHttpContext(antiforgeryValidationPassed: false, sessionToken: "jwt-token"), _authApi.Object);

        // Assert
        result.Should().BeAssignableTo<IStatusCodeHttpResult>().Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        _authApi.Verify(a => a.LogoutAsync(It.IsAny<string>()), Times.Never);
        _authenticationService.Verify(s => s.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties?>()), Times.Never);
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

    private static LoginRequest CreateRequest() => new() { Email = "ploew@example.com", Password = "12345" };

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

    private static async Task<ApiException> CreateUnauthorizedException()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost/api/auth/login");
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("", Encoding.UTF8, "application/json") };
        return await ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    private readonly Mock<IAuthApi> _authApi = new();
    private readonly Mock<IAuthenticationService> _authenticationService = new();

    // The framework's own implementation is internal; the interface is all the handlers depend on.
    private sealed class StubAntiforgeryValidationFeature(bool isValid) : IAntiforgeryValidationFeature
    {
        public bool IsValid => isValid;
        public Exception? Error => null;
    }
}
