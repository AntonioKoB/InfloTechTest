using System;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Commands;

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
    public DeleteUserCommandHandler(IUserService userService, IUserLogService userLogService)
    {
    }

    public Task<long> HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
}
