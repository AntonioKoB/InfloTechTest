using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace UserManagement.Services.Commands;

/// <summary>
/// Statuses in a dictionary: lost on restart, per instance, never expired. Enough for a single instance.
/// </summary>
public class InMemoryCommandStatusStore : ICommandStatusStore
{
    private readonly ConcurrentDictionary<Guid, CommandStatus> _statuses = new();

    public Task MarkPendingAsync(Guid commandId)
    {
        _statuses[commandId] = new CommandStatus(commandId, CommandState.Pending, null, null);
        return Task.CompletedTask;
    }

    public Task MarkCompletedAsync(Guid commandId, long userId)
    {
        _statuses[commandId] = new CommandStatus(commandId, CommandState.Completed, userId, null);
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid commandId, string error)
    {
        _statuses[commandId] = new CommandStatus(commandId, CommandState.Failed, null, error);
        return Task.CompletedTask;
    }

    public Task<CommandStatus?> GetAsync(Guid commandId)
        => Task.FromResult(_statuses.TryGetValue(commandId, out var status) ? status : null);
}
