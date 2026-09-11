using System.Threading;
using UserManagement.Api.Contracts.Commands;

namespace UserManagement.Blazor.Api;

public class CommandPoller : ICommandPoller
{
    public CommandPoller(IUsersApi usersApi, TimeSpan interval, TimeSpan timeout)
    {
    }

    public Task<CommandStatusResponse> WaitForOutcomeAsync(Guid commandId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
