using System;

namespace UserManagement.Services.Commands;

public sealed record DeleteUserCommand(Guid CommandId, long UserId) : ICommand;
