using System;

namespace UserManagement.Services.Commands;

/// <summary>
/// Where a command is in its life: Pending from the moment the API accepts it, then Completed with the id of
/// the user it affected, or Failed with the reason.
/// </summary>
public sealed record CommandStatus(Guid CommandId, CommandState State, long? UserId, string? Error);
