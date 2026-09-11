namespace UserManagement.Api.Contracts.Commands;

/// <summary>
/// Body of a 202: the command was queued. Poll GET /api/commands/{CommandId} (also the Location header) for
/// the outcome.
/// </summary>
public class CommandAcceptedResponse
{
    public Guid CommandId { get; set; }
}
