using System;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Commands;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    public CreateUserCommandHandler(IUserService userService, IUserLogService userLogService)
    {
    }

    public Task<long> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
}
