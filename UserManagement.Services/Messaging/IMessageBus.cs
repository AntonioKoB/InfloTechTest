using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Services.Commands;

namespace UserManagement.Services.Messaging;

/// <summary>
/// The transport between the API (publisher) and the worker (consumer). The in-memory implementation is a
/// channel inside the API process; a broker-backed one (Azure Service Bus) fits the same two calls.
/// </summary>
public interface IMessageBus
{
    Task PublishAsync(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commands in the order they were published; waits when there are none and ends only on cancellation.
    /// </summary>
    IAsyncEnumerable<ICommand> ConsumeAsync(CancellationToken cancellationToken);
}
