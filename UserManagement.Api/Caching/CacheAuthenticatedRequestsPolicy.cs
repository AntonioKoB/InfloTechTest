using System.Threading;
using Microsoft.AspNetCore.OutputCaching;

namespace UserManagement.Api.Caching;

public sealed class CacheAuthenticatedRequestsPolicy : IOutputCachePolicy
{
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
