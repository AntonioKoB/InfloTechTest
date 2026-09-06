using System.Linq;
using System.Threading.Tasks;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Web.Models.Users;

namespace UserManagement.WebMS.Controllers;

[Route("users")]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    public UsersController(IUserService userService) => _userService = userService;

    [HttpGet]
    public async Task<ViewResult> List(UserListFilter filter = UserListFilter.All)
    {
        var users = filter switch
        {
            UserListFilter.Active => await _userService.FilterByActiveAsync(true),
            UserListFilter.NonActive => await _userService.FilterByActiveAsync(false),
            _ => await _userService.GetAllAsync()
        };

        var model = new UserListViewModel
        {
            Items = [.. users.Select(p => p.ToListItemViewModel())]
        };

        return View(model);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> View(long id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null) return NotFound();

        return View(user.ToListItemViewModel());
    }
}
