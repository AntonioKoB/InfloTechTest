using System;
using UserManagement.Models;

namespace UserManagement.Services.Commands;

/// <summary>
/// Persist an audit entry built where the action happened.
/// </summary>
public sealed record RecordUserLogCommand(Guid CommandId, UserLog Entry) : ICommand;
