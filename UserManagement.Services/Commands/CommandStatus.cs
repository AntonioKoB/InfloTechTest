using System;

namespace UserManagement.Services.Commands;

/// <summary>
/// Pending once accepted, then Completed with the affected user id or Failed with the reason.
/// </summary>
public sealed record CommandStatus(Guid CommandId, CommandState State, long? UserId, string? Error);
