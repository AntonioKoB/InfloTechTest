using System.Threading;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Domain.Exceptions;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Commands;

public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
{
    private readonly IUserService _userService;
    private readonly IUserLogService _userLogService;

    public UpdateUserCommandHandler(IUserService userService, IUserLogService userLogService)
    {
        _userService = userService;
        _userLogService = userLogService;
    }

    public async Task<long> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(command.UserId) ?? throw new UserNoLongerExistsException(command.UserId);

        // The fetched instance is mutated in place and saved, so the "before" snapshot is a copy taken now.
        var before = user.Clone();
        user.Forename = command.Forename;
        user.Surname = command.Surname;
        user.Email = command.Email;
        user.DateOfBirth = command.DateOfBirth;
        user.IsActive = command.IsActive;
        if (command.PasswordHash is not null)
            user.PasswordHash = command.PasswordHash;

        await _userService.UpdateAsync(user);
        await _userLogService.RecordAsync(user.Id, UserLogAction.Updated, before, after: user);
        return user.Id;
    }
}
