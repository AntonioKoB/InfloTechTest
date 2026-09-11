using System.Threading;
using UserManagement.Api.Contracts.Commands;

namespace UserManagement.Blazor.Api;

/// <summary>
/// Waits for an accepted command to finish by polling the API's status endpoint.
/// </summary>
public interface ICommandPoller
{
    /// <summary>
    /// Returns the first status that is no longer Pending, or throws TimeoutException.
    /// </summary>
    Task<CommandStatusResponse> WaitForOutcomeAsync(Guid commandId, CancellationToken cancellationToken = default);
}
