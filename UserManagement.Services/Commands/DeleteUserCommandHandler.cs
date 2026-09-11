using System.Threading;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Commands;

/// <summary>
/// Idempotent delete. Records a Deleted entry with the last known state when there was a user to delete.
/// </summary>
public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
    private readonly IUserService _userService;
    private readonly IUserLogService _userLogService;

    public DeleteUserCommandHandler(IUserService userService, IUserLogService userLogService)
    {
        _userService = userService;
        _userLogService = userLogService;
    }

    public async Task<long> HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var before = await _userService.GetByIdAsync(command.UserId);
        await _userService.DeleteAsync(command.UserId);
        if (before is not null)
            await _userLogService.RecordAsync(command.UserId, UserLogAction.Deleted, before, after: null);
        return command.UserId;
    }
}
