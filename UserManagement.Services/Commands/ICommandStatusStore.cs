using System;
using System.Threading.Tasks;

namespace UserManagement.Services.Commands;

/// <summary>
/// Tracks the outcome of accepted commands for the status endpoint.
/// </summary>
public interface ICommandStatusStore
{
    Task MarkPendingAsync(Guid commandId);
    Task MarkCompletedAsync(Guid commandId, long userId);
    Task MarkFailedAsync(Guid commandId, string error);
    Task<CommandStatus?> GetAsync(Guid commandId);
}
