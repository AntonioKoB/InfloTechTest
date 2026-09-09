using UserManagement.Api.Contracts.Logs;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Api.Controllers;

[ApiController]
[Route("api/logs")]
public class LogsController : ControllerBase
{
    private readonly IUserLogService _userLogService;
    private readonly IUserLogDiffBuilder _diffBuilder;

    public LogsController(IUserLogService userLogService, IUserLogDiffBuilder diffBuilder)
    {
        _userLogService = userLogService;
        _diffBuilder = diffBuilder;
    }

    [HttpGet]
    public Task<ActionResult<PagedResultDto<UserLogDto>>> GetLogs(int page = 1, int pageSize = 10)
        => throw new NotImplementedException();

    [HttpGet("{id:long}")]
    public Task<ActionResult<UserLogDto>> GetById(long id)
        => throw new NotImplementedException();
}
