namespace UserManagement.Api.Contracts.Commands;

/// <summary>
/// Outcome of an accepted command. UserId is set once Completed; Error carries the reason when Failed.
/// </summary>
public class CommandStatusResponse
{
    public Guid CommandId { get; set; }
    public CommandState State { get; set; }
    public long? UserId { get; set; }
    public string? Error { get; set; }
}
