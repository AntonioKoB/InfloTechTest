using System;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Commands;

public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
{
    public UpdateUserCommandHandler(IUserService userService, IUserLogService userLogService)
    {
    }

    public Task<long> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
}
