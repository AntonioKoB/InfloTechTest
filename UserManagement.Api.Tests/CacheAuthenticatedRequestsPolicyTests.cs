using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using UserManagement.Api.Caching;

namespace UserManagement.Api.Tests;

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
    public async Task ServeResponseAsync_WhenAuthenticated200WithoutCookies_MustAllowStorage()
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
