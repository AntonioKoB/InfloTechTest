using System;
using System.Threading.Tasks;

namespace UserManagement.Services.Commands;

/// <summary>
/// Tracks the outcome of accepted commands for the status endpoint. Asynchronous so a durable store (a table)
/// can replace the in-memory one without touching its callers.
/// </summary>
public interface ICommandStatusStore
{
    Task MarkPendingAsync(Guid commandId);
    Task MarkCompletedAsync(Guid commandId, long userId);
    Task MarkFailedAsync(Guid commandId, string error);
    Task<CommandStatus?> GetAsync(Guid commandId);
}
