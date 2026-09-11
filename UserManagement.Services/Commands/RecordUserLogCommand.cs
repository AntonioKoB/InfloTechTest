using System;
using UserManagement.Models;

namespace UserManagement.Services.Commands;

/// <summary>
/// Persist a finished audit entry. The entry is built where the action happened (snapshots already
/// serialized), so the handler only has to write it.
/// </summary>
public sealed record RecordUserLogCommand(Guid CommandId, UserLog Entry) : ICommand;
