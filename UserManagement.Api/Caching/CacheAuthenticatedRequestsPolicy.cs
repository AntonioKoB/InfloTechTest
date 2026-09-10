using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace UserManagement.Api.Caching;

/// <summary>
/// Opts an endpoint back into output caching for authenticated callers. The framework's default policy
/// refuses to cache a request carrying an Authorization header, and refuses again at response time when
/// the user turned out to be authenticated - the safe assumption that an authenticated response is personal.
/// The users list is not: every signed-in caller gets the same rows, so one shared cached response is
/// correct. Appended after the default policy, this re-allows lookup and storage for a GET, and at response
/// time only under the default policy's other two rules: a 200 that sets no cookie.
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
