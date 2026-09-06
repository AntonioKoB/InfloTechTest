using System;
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
    public IActionResult Add() => View();

    [HttpPost("add")]
    public async Task<IActionResult> Add(UserFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _userService.CreateAsync(model.ToUser());
        }
        catch (EmailAlreadyExistsException)
        {
            ModelState.AddModelError(nameof(UserFormViewModel.Email), "A user with this email already exists.");
            return View(model);
        }

        return RedirectToAction(nameof(List));
    }

    [HttpGet("edit/{id:long}")]
    public Task<IActionResult> Edit(long id) => throw new NotImplementedException();

    [HttpPost("edit/{id:long}")]
    public Task<IActionResult> Edit(long id, UserFormViewModel model) => throw new NotImplementedException();
}
