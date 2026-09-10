using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace UserManagement.Api.Caching;

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
        // The default policy vetoes storage again here for an authenticated user. Re-allow it under its other
        // two rules only: a 200 that sets no cookie.
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
