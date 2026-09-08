using System.Linq;
using System.Threading.Tasks;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Web.Models.Logs;

namespace UserManagement.WebMS.Controllers;

[Route("logs")]
public class LogsController : Controller
{
    private const int PageSize = 20;

    private readonly IUserLogService _userLogService;
    public LogsController(IUserLogService userLogService) => _userLogService = userLogService;

    [HttpGet]
    public async Task<ViewResult> List(int page = 1)
    {
        var result = await _userLogService.GetPagedAsync(page, PageSize);

        var model = new LogListViewModel
        {
            Items = [.. result.Items.Select(l => new LogListItemViewModel
            {
                UserId = l.UserId,
                Action = l.Action,
                Timestamp = l.Timestamp
            })],
            Page = result.Page,
            TotalPages = result.TotalPages
        };

        return View(model);
    }
}
