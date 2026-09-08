using System;
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
    public Task<ViewResult> List(int page = 1) => throw new NotImplementedException();
}
