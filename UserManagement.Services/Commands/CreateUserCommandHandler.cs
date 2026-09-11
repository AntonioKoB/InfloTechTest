using System.Threading;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Commands;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    private readonly IUserService _userService;
    private readonly IUserLogService _userLogService;

    public CreateUserCommandHandler(IUserService userService, IUserLogService userLogService)
    {
        _userService = userService;
        _userLogService = userLogService;
    }

    public async Task<long> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Forename = command.Forename,
            Surname = command.Surname,
            Email = command.Email,
            DateOfBirth = command.DateOfBirth,
            IsActive = command.IsActive,
            PasswordHash = command.PasswordHash
        };

        await _userService.CreateAsync(user);
        await _userLogService.RecordAsync(user.Id, UserLogAction.Created, before: null, after: user);
        return user.Id;
    }
}
