using System;

namespace UserManagement.Services.Commands;

/// <summary>
/// Update a user. A null PasswordHash means "keep the current one".
/// </summary>
public sealed record UpdateUserCommand(Guid CommandId, long UserId, string Forename, string Surname, string Email, DateOnly DateOfBirth, bool IsActive, string? PasswordHash) : ICommand;
