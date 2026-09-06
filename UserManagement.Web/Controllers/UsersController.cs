using System.Linq;
using System.Threading.Tasks;
using UserManagement.Services.Domain.Exceptions;
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

    [HttpGet("{id:long}")]
    public async Task<IActionResult> View(long id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null) return View("UserNotFound", id);

        return View(user.ToViewModel());
    }

    [HttpGet("add")]
    public IActionResult Add()
    {
        SetFormViewData(nameof(Add));
        return View("UserForm", new UserFormViewModel());
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add(UserFormViewModel model)
    {
        SetFormViewData(nameof(Add));

        if (!ModelState.IsValid) return View("UserForm", model);

        try
        {
            await _userService.CreateAsync(model.ToUser());
        }
        catch (EmailAlreadyExistsException)
        {
            ModelState.AddModelError(nameof(UserFormViewModel.Email), "A user with this email already exists.");
            return View("UserForm", model);
        }

        return RedirectToAction(nameof(List));
    }

    [HttpGet("edit/{id:long}")]
    public async Task<IActionResult> Edit(long id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null) return View("UserNotFound", id);

        SetFormViewData(nameof(Edit), id);
        return View("UserForm", user.ToFormViewModel());
    }

    [HttpPost("edit/{id:long}")]
    public async Task<IActionResult> Edit(long id, UserFormViewModel model)
    {
        SetFormViewData(nameof(Edit), id);

        if (!ModelState.IsValid) return View("UserForm", model);

        await _userService.UpdateAsync(model.ToUser(id));

        return RedirectToAction(nameof(List));
    }

    private void SetFormViewData(string formAction, long? userId = null)
    {
        var isEdit = formAction == nameof(Edit);
        ViewData["Title"] = isEdit ? "Edit User" : "Add User";
        ViewData["SubmitButtonText"] = isEdit ? "Save Changes" : "Add User";
        ViewData["FormAction"] = formAction;
        ViewData["UserId"] = userId;
    }
}
