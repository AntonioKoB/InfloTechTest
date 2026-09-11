namespace UserManagement.Api.Contracts.Commands;

/// <summary>
/// The body of a 202 Accepted: the command was queued, not executed. Poll GET /api/commands/{CommandId}
/// (also given as the response's Location header) until it reports Completed or Failed.
/// </summary>
public class CommandAcceptedResponse
{
    public Guid CommandId { get; set; }
}
