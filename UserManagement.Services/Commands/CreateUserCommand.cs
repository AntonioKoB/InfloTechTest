using System;

namespace UserManagement.Services.Commands;

/// <summary>
/// Create a user. Carries the password hash, never the clear-text password.
/// </summary>
public sealed record CreateUserCommand(Guid CommandId, string Forename, string Surname, string Email, DateOnly DateOfBirth, bool IsActive, string PasswordHash) : ICommand;
