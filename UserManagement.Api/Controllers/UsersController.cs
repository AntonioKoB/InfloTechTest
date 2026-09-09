using UserManagement.Api.Contracts.Logs;
using UserManagement.Api.Contracts.Users;
using UserManagement.Api.Mapping;
using UserManagement.Services.Domain.Exceptions;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserLogService _userLogService;
    private readonly ICredentialService _credentialService;

    public UsersController(IUserService userService, IUserLogService userLogService, ICredentialService credentialService)
    {
        _userService = userService;
        _userLogService = userLogService;
        _credentialService = credentialService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(UserListFilter filter = UserListFilter.All)
    {
        var users = filter switch
        {
            UserListFilter.Active => await _userService.FilterByActiveAsync(true),
            UserListFilter.NonActive => await _userService.FilterByActiveAsync(false),
            _ => await _userService.GetAllAsync()
        };

        return Ok(users.Select(u => u.ToDto()));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<UserDto>> GetById(long id, bool recordAsViewed = false)
    {
        var user = await _userService.GetByIdAsync(id, recordAsViewed);
        if (user is null) return NotFound();

        return Ok(user.ToDto());
    }

    [HttpGet("{id:long}/logs")]
    public async Task<ActionResult<IEnumerable<UserLogDto>>> GetUserLogs(long id)
    {
        var logs = await _userLogService.GetForUserAsync(id);
        return Ok(logs.Select(l => l.ToDto()));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request)
    {
        var user = request.ToUser();
        _credentialService.SetPassword(user, request.Password);

        try
        {
            await _userService.CreateAsync(user);
        }
        catch (EmailAlreadyExistsException ex)
        {
            return EmailConflict(ex);
        }

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user.ToDto());
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<UserDto>> Update(long id, UpdateUserRequest request)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null) return NotFound();

        request.ApplyTo(user);
        if (!string.IsNullOrEmpty(request.Password))
        {
            _credentialService.SetPassword(user, request.Password);
        }

        try
        {
            await _userService.UpdateAsync(user);
        }
        catch (EmailAlreadyExistsException ex)
        {
            return EmailConflict(ex);
        }
        catch (UserNoLongerExistsException)
        {
            return NotFound();
        }

        return Ok(user.ToDto());
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _userService.DeleteAsync(id);
        return NoContent();
    }

    private ActionResult EmailConflict(EmailAlreadyExistsException ex)
    {
        ModelState.AddModelError(nameof(UserDto.Email), ex.Message);
        return ValidationProblem(ModelState);
    }
}
