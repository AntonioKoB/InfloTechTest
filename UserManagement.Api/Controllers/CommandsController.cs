using Microsoft.Extensions.Logging;
using UserManagement.Api.Contracts.Commands;
using UserManagement.Api.Mapping;
using UserManagement.Services.Commands;

namespace UserManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/commands")]
public partial class CommandsController : ControllerBase
{
    private readonly ICommandStatusStore _statusStore;
    private readonly ILogger<CommandsController> _logger;

    public CommandsController(ICommandStatusStore statusStore, ILogger<CommandsController> logger)
    {
        _statusStore = statusStore;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CommandStatusResponse>> GetStatus(Guid id)
    {
        var status = await _statusStore.GetAsync(id);
        if (status is null)
        {
            LogCommandNotFound(id);
            return NotFound();
        }

        return Ok(status.ToResponse());
    }

    [LoggerMessage(EventId = 1301, Level = LogLevel.Information, Message = "Command {CommandId} was not found")]
    private partial void LogCommandNotFound(Guid commandId);
}
