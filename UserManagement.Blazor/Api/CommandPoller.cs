using System.Threading;
using UserManagement.Api.Contracts.Commands;

namespace UserManagement.Blazor.Api;

/// <summary>
/// Polls a command's status until it is Completed or Failed, or throws TimeoutException once the timeout
/// passes. An API error propagates as it is.
/// </summary>
public class CommandPoller : ICommandPoller
{
    private readonly IUsersApi _usersApi;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _timeout;

    public CommandPoller(IUsersApi usersApi, TimeSpan interval, TimeSpan timeout)
    {
        _usersApi = usersApi;
        _interval = interval;
        _timeout = timeout;
    }

    public async Task<CommandStatusResponse> WaitForOutcomeAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + _timeout;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = await _usersApi.GetCommandStatusAsync(commandId);
            if (status.State != CommandState.Pending)
                return status;

            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException($"Command {commandId} was still pending after {_timeout.TotalSeconds:0} seconds.");

            await Task.Delay(_interval, cancellationToken);
        }
    }
}
