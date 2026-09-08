using System.Linq;
using System.Threading.Tasks;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Web.Models.Logs;

namespace UserManagement.WebMS.Controllers;

[Route("logs")]
public class LogsController : Controller
{
    private const int PageSize = 10;

    private readonly IUserLogService _userLogService;
    private readonly IUserLogDiffBuilder _diffBuilder;
    public LogsController(IUserLogService userLogService, IUserLogDiffBuilder diffBuilder)
    {
        _userLogService = userLogService;
        _diffBuilder = diffBuilder;
    }

    [HttpGet]
    public async Task<ViewResult> List(int page = 1)
    {
        var result = await _userLogService.GetPagedAsync(page, PageSize);

        var model = new LogListViewModel
        {
            Items = [.. result.Items.Select(l => l.ToListItemViewModel())],
            Page = result.Page,
            TotalPages = result.TotalPages
        };

        return View(model);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> View(long id)
    {
        var log = await _userLogService.GetByIdAsync(id);

        return View(new LogDetailViewModel
        {
            UserId = log!.UserId,
            Action = log.Action,
            Timestamp = log.Timestamp,
            Changes = [.. _diffBuilder.Build(log).Select(c => c.ToViewModel())]
        });
    }
}
