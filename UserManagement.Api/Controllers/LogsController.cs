using UserManagement.Api.Contracts.Logs;
using UserManagement.Api.Mapping;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Api.Controllers;

[ApiController]
[Authorize]
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
    public async Task<ActionResult<PagedResultDto<UserLogDto>>> GetLogs(int page = 1, int pageSize = 10)
    {
        var result = await _userLogService.GetPagedAsync(page, pageSize);

        return Ok(new PagedResultDto<UserLogDto>
        {
            Items = [.. result.Items.Select(l => l.ToDto())],
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        });
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<UserLogDto>> GetById(long id)
    {
        var log = await _userLogService.GetByIdAsync(id);
        if (log is null) return NotFound();

        var changes = _diffBuilder.Build(log);
        return Ok(log.ToDto(changes));
    }
}
