using Microsoft.Extensions.Logging;
using UserManagement.Api.Contracts.Commands;
using UserManagement.Services.Commands;

namespace UserManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/commands")]
public class CommandsController : ControllerBase
{
    public CommandsController(ICommandStatusStore statusStore, ILogger<CommandsController> logger)
    {
    }

    [HttpGet("{id:guid}")]
    public Task<ActionResult<CommandStatusResponse>> GetStatus(Guid id) => throw new NotImplementedException();
}
