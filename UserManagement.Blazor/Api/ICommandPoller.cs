using System.Threading;
using UserManagement.Api.Contracts.Commands;

namespace UserManagement.Blazor.Api;

/// <summary>
/// Waits for an accepted command to finish by polling the API's status endpoint.
/// </summary>
public interface ICommandPoller
{
    /// <summary>
    /// Returns the first status that is no longer Pending. Throws <see cref="TimeoutException"/> when the
    /// command is still Pending after the poller's timeout, so a lost command never looks like a slow one.
    /// </summary>
    Task<CommandStatusResponse> WaitForOutcomeAsync(Guid commandId, CancellationToken cancellationToken = default);
}
