using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using UserManagement.Blazor.Auth;

namespace UserManagement.Blazor.Tests;

public class BearerTokenHandlerTests
{
    [Fact]
    public async Task SendAsync_WhenTheSignedInUserHasAnAccessToken_MustSendItAsABearerAuthorizationHeader()
    {
        // Arrange
        var client = CreateClient(SignedInWithToken("jwt-token"), respondWith: HttpStatusCode.OK);

        // Act
        await client.GetAsync("https://api.test/api/users");

        // Assert
        _inner.LastRequest!.Headers.Authorization.Should().BeEquivalentTo(new AuthenticationHeaderValue("Bearer", "jwt-token"));
    }

    [Fact]
    public async Task SendAsync_WhenAnonymous_MustNotSendAnAuthorizationHeader()
    {
        // Arrange
        var client = CreateClient(Anonymous(), respondWith: HttpStatusCode.OK);

        // Act
        await client.GetAsync("https://api.test/api/users");

        // Assert
        _inner.LastRequest!.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WhenTheApiRejectsTheToken_MustForceAFullReloadOfTheLoginPage()
    {
        // Arrange
        // A 401 from the API means the token is no longer accepted (typically expired). The cookie expires
        // with the token, so a full page load - not a client-side route change - is what clears the stale
        // circuit state and lands the user on the login page.
        var client = CreateClient(SignedInWithToken("expired-token"), respondWith: HttpStatusCode.Unauthorized);

        // Act
        await client.GetAsync("https://api.test/api/users");

        // Assert
        _navigation.Navigations.Should().ContainSingle()
            .Which.Should().Be(("/login", true));
    }

    [Fact]
    public async Task SendAsync_WhenTheApiAcceptsTheToken_MustNotNavigate()
    {
        // Arrange
        var client = CreateClient(SignedInWithToken("jwt-token"), respondWith: HttpStatusCode.OK);

        // Act
        await client.GetAsync("https://api.test/api/users");

        // Assert
        _navigation.Navigations.Should().BeEmpty();
    }

    private static ClaimsPrincipal SignedInWithToken(string token)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Peter Loew"), new Claim(AuthClaimTypes.AccessToken, token)], "Test"));

    private static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    private HttpClient CreateClient(ClaimsPrincipal user, HttpStatusCode respondWith)
    {
        _inner.RespondWith = respondWith;
        var handler = new BearerTokenHandler(new FakeAuthenticationStateProvider(user), _navigation) { InnerHandler = _inner };
        return new HttpClient(handler);
    }

    private readonly RecordingHandler _inner = new();
    private readonly TestNavigationManager _navigation = new();

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public HttpStatusCode RespondWith { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(RespondWith));
        }
    }

    private sealed class FakeAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public List<(string Uri, bool ForceLoad)> Navigations { get; } = [];

        public TestNavigationManager() => Initialize("https://app.test/", "https://app.test/users");

        protected override void NavigateToCore(string uri, bool forceLoad) => Navigations.Add((uri, forceLoad));
    }
}
