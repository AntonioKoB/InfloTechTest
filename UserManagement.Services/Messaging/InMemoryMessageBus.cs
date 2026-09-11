using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using UserManagement.Services.Commands;

namespace UserManagement.Services.Messaging;

/// <summary>
/// An unbounded in-process channel with one consumer. Nothing survives a restart or is shared across
/// instances.
/// </summary>
public class InMemoryMessageBus : IMessageBus
{
    private readonly Channel<ICommand> _channel = Channel.CreateUnbounded<ICommand>(new UnboundedChannelOptions { SingleReader = true });

    public Task PublishAsync(ICommand command, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(command, cancellationToken).AsTask();

    public IAsyncEnumerable<ICommand> ConsumeAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
