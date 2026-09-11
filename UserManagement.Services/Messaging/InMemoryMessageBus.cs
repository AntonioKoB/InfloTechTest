using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Services.Commands;

namespace UserManagement.Services.Messaging;

public class InMemoryMessageBus : IMessageBus
{
    public Task PublishAsync(ICommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public IAsyncEnumerable<ICommand> ConsumeAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
