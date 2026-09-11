using System;

namespace UserManagement.Services.Commands;

/// <summary>
/// A unit of work accepted by the API and executed later by the worker. The id is what the caller polls.
/// </summary>
public interface ICommand
{
    Guid CommandId { get; }
}
