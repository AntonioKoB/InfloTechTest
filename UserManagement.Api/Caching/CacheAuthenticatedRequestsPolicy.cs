using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace UserManagement.Api.Caching;

/// <summary>
/// Re-allows output caching for authenticated callers on an endpoint whose response is the same for every
/// signed-in user. The default policy refuses any request carrying an Authorization header; this keeps its
/// other rules (a GET, a 200, no Set-Cookie).
/// </summary>
public sealed class CacheAuthenticatedRequestsPolicy : IOutputCachePolicy
{
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        if (HttpMethods.IsGet(context.HttpContext.Request.Method))
        {
            context.AllowCacheLookup = true;
            context.AllowCacheStorage = true;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        if (HttpMethods.IsGet(context.HttpContext.Request.Method)
            && response.StatusCode == StatusCodes.Status200OK
            && StringValues.IsNullOrEmpty(response.Headers.SetCookie))
        {
            context.AllowCacheStorage = true;
        }

        return ValueTask.CompletedTask;
    }
}
