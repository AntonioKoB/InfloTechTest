using System;
using System.Threading.Tasks;

namespace UserManagement.Services.Commands;

public class InMemoryCommandStatusStore : ICommandStatusStore
{
    public Task MarkPendingAsync(Guid commandId) => throw new NotImplementedException();
    public Task MarkCompletedAsync(Guid commandId, long userId) => throw new NotImplementedException();
    public Task MarkFailedAsync(Guid commandId, string error) => throw new NotImplementedException();
    public Task<CommandStatus?> GetAsync(Guid commandId) => throw new NotImplementedException();
}
