namespace UserManagement.Api.Contracts.Commands;

/// <summary>
/// The outcome of an accepted command. UserId is set once the command Completed (for a create, this is the
/// only place the new id appears); Error carries the reason when it Failed.
/// </summary>
public class CommandStatusResponse
{
    public Guid CommandId { get; set; }
    public CommandState State { get; set; }
    public long? UserId { get; set; }
    public string? Error { get; set; }
}
