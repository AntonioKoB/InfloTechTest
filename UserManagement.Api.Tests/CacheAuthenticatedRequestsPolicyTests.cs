using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using UserManagement.Api.Caching;

namespace UserManagement.Api.Tests;

/// <summary>
/// The framework's default output-cache policy switches caching off for authenticated callers twice: at
/// request time, for any request carrying an Authorization header, and again at response time, for any
/// request whose user turned out to be authenticated. Every call to this API is both, so it would all go
/// uncached. This policy is appended to the users list policy to opt that one endpoint back in at both
/// points: the list is the same for every signed-in caller, so one shared cached response is correct. It
/// must only ever do so for a GET, and at response time only under the default policy's other rules (a
/// 200 that sets no cookie).
/// </summary>
public class CacheAuthenticatedRequestsPolicyTests
{
    [Fact]
    public async Task CacheRequestAsync_WhenRequestIsAGetWithABearerToken_MustAllowLookupAndStorage()
    {
        // Arrange
        var context = CreateContextLeftByTheDefaultPolicy(HttpMethods.Get);

        // Act
        await CreatePolicy().CacheRequestAsync(context, CancellationToken.None);

        // Assert
        context.AllowCacheLookup.Should().BeTrue();
        context.AllowCacheStorage.Should().BeTrue();
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task CacheRequestAsync_WhenRequestIsNotAGet_MustLeaveLookupAndStorageDisabled(string method)
    {
        // Arrange
        var context = CreateContextLeftByTheDefaultPolicy(method);

        // Act
        await CreatePolicy().CacheRequestAsync(context, CancellationToken.None);

        // Assert
        context.AllowCacheLookup.Should().BeFalse();
        context.AllowCacheStorage.Should().BeFalse();
    }

    [Fact]
    public async Task ServeResponseAsync_WhenAuthenticatedResponseIsA200WithoutCookies_MustAllowStorage()
    {
        // Arrange
        var context = CreateContextLeftByTheDefaultPolicy(HttpMethods.Get);
        context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;

        // Act
        await CreatePolicy().ServeResponseAsync(context, CancellationToken.None);

        // Assert
        context.AllowCacheStorage.Should().BeTrue();
    }

    [Theory]
    [InlineData(StatusCodes.Status404NotFound)]
    [InlineData(StatusCodes.Status500InternalServerError)]
    public async Task ServeResponseAsync_WhenResponseIsNotA200_MustLeaveStorageDisabled(int statusCode)
    {
        // Arrange
        var context = CreateContextLeftByTheDefaultPolicy(HttpMethods.Get);
        context.HttpContext.Response.StatusCode = statusCode;

        // Act
        await CreatePolicy().ServeResponseAsync(context, CancellationToken.None);

        // Assert
        context.AllowCacheStorage.Should().BeFalse();
    }

    [Fact]
    public async Task ServeResponseAsync_WhenResponseSetsACookie_MustLeaveStorageDisabled()
    {
        // Arrange
        var context = CreateContextLeftByTheDefaultPolicy(HttpMethods.Get);
        context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        context.HttpContext.Response.Headers.SetCookie = "session=abc";

        // Act
        await CreatePolicy().ServeResponseAsync(context, CancellationToken.None);

        // Assert
        context.AllowCacheStorage.Should().BeFalse();
    }

    /// <summary>
    /// The state the framework's default policy leaves behind for an authenticated request: caching enabled
    /// for the endpoint, but lookup and storage both switched off because of the Authorization header.
    /// </summary>
    private static OutputCacheContext CreateContextLeftByTheDefaultPolicy(string method)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Headers.Authorization = "Bearer some-token";

        return new OutputCacheContext
        {
            HttpContext = httpContext,
            EnableOutputCaching = true,
            AllowCacheLookup = false,
            AllowCacheStorage = false
        };
    }

    private static IOutputCachePolicy CreatePolicy() => new CacheAuthenticatedRequestsPolicy();
}
