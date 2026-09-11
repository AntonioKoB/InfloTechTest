using UserManagement.Api.Contracts.Commands;
using UserManagement.Models;
using UserManagement.Services.Commands;
using ContractCommandState = UserManagement.Api.Contracts.Commands.CommandState;
using DomainCommandState = UserManagement.Services.Commands.CommandState;

namespace UserManagement.Api.Mapping;

public static class CommandMappingExtensions
{
    // The user has had its password hashed by the time it is turned into a command.
    public static CreateUserCommand ToCreateCommand(this User user, Guid commandId)
        => new(commandId, user.Forename, user.Surname, user.Email, user.DateOfBirth, user.IsActive, user.PasswordHash!);

    public static UpdateUserCommand ToUpdateCommand(this User user, Guid commandId, string? passwordHash)
        => new(commandId, user.Id, user.Forename, user.Surname, user.Email, user.DateOfBirth, user.IsActive, passwordHash);

    public static CommandStatusResponse ToResponse(this CommandStatus status) => new()
    {
        CommandId = status.CommandId,
        State = status.State switch
        {
            DomainCommandState.Pending => ContractCommandState.Pending,
            DomainCommandState.Completed => ContractCommandState.Completed,
            DomainCommandState.Failed => ContractCommandState.Failed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status.State, "Unknown command state")
        },
        UserId = status.UserId,
        Error = status.Error
    };
}
