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
    private readonly IUserLogService _userLogService;
    public UsersController(IUserService userService, IUserLogService userLogService)
    {
        _userService = userService;
        _userLogService = userLogService;
    }

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
        var user = await _userService.GetByIdAsync(id, recordAsViewed: true);
        if (user is null) return View("UserNotFound", id);

        var logs = await _userLogService.GetForUserAsync(id);

        var model = user.ToViewModel();
        model.Logs = [.. logs.Select(l => new UserLogEntryViewModel { Action = l.Action, Timestamp = l.Timestamp })];
        return View(model);
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
        var existingUser = await _userService.GetByIdAsync(id);
        if (existingUser is null) return View("UserNotFound", id);

        SetFormViewData(nameof(Edit), id);

        if (!ModelState.IsValid) return View("UserForm", model);

        model.ApplyTo(existingUser);

        try
        {
            await _userService.UpdateAsync(existingUser);
        }
        catch (EmailAlreadyExistsException)
        {
            ModelState.AddModelError(nameof(UserFormViewModel.Email), "A user with this email already exists.");
            return View("UserForm", model);
        }

        return RedirectToAction(nameof(List));
    }

    [HttpGet("delete/{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null) return View("UserNotFound", id);

        return View(user.ToViewModel());
    }

    [HttpDelete("delete/{id:long}")]
    [ActionName(nameof(Delete))]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        await _userService.DeleteAsync(id);
        return Ok();
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
