using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Services.Commands;

namespace UserManagement.Services.Messaging;

/// <summary>
/// Transport between the API (publisher) and the worker (consumer).
/// </summary>
public interface IMessageBus
{
    Task PublishAsync(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commands in publish order; waits when there are none, ends on cancellation.
    /// </summary>
    IAsyncEnumerable<ICommand> ConsumeAsync(CancellationToken cancellationToken);
}
