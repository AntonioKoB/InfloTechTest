using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using UserManagement.Api.Caching;

namespace UserManagement.Api.Tests;

/// <summary>
/// The framework's default output-cache policy switches caching off for any request that carries an
/// Authorization header, so every call to this API would go uncached. This policy is appended to the users
/// list policy to opt that one endpoint back in: the list is the same for every signed-in caller, so one
/// shared cached response is correct. It must only ever do so for a GET.
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
