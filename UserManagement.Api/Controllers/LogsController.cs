using Microsoft.Extensions.Logging;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Api.Mapping;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/logs")]
public partial class LogsController : ControllerBase
{
    private readonly IUserLogService _userLogService;
    private readonly IUserLogDiffBuilder _diffBuilder;
    private readonly ILogger<LogsController> _logger;

    public LogsController(IUserLogService userLogService, IUserLogDiffBuilder diffBuilder, ILogger<LogsController> logger)
    {
        _userLogService = userLogService;
        _diffBuilder = diffBuilder;
        _logger = logger;
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
        if (log is null)
        {
            LogEntryNotFound(id);
            return NotFound();
        }

        var changes = _diffBuilder.Build(log);
        return Ok(log.ToDto(changes));
    }

    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "Log entry {LogId} was not found")]
    private partial void LogEntryNotFound(long logId);
}
