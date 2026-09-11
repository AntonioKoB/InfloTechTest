using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using UserManagement.Api.Caching;
using UserManagement.Api.Contracts.Commands;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Api.Contracts.Users;
using UserManagement.Api.Mapping;
using UserManagement.Services.Commands;
using UserManagement.Services.Domain.Exceptions;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Services.Messaging;

namespace UserManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public partial class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserLogService _userLogService;
    private readonly ICredentialService _credentialService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, IUserLogService userLogService, ICredentialService credentialService, IMessageBus messageBus, ICommandStatusStore statusStore, ILogger<UsersController> logger)
    {
        _userService = userService;
        _userLogService = userLogService;
        _credentialService = credentialService;
        _logger = logger;
    }

    [HttpGet]
    [OutputCache(PolicyName = OutputCachingExtensions.UsersListPolicy)]
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
        if (user is null)
        {
            LogUserNotFound(id);
            return NotFound();
        }

        return Ok(user.ToDto());
    }

    [HttpGet("{id:long}/logs")]
    public async Task<ActionResult<IEnumerable<UserLogDto>>> GetUserLogs(long id)
    {
        var logs = await _userLogService.GetForUserAsync(id);
        return Ok(logs.Select(l => l.ToDto()));
    }

    [HttpPost]
    public async Task<ActionResult<CommandAcceptedResponse>> Create(CreateUserRequest request)
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
    public async Task<ActionResult<CommandAcceptedResponse>> Update(long id, UpdateUserRequest request)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null)
        {
            LogUserNotFound(id);
            return NotFound();
        }

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
            LogUserNoLongerExists(id);
            return NotFound();
        }

        return Ok(user.ToDto());
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<CommandAcceptedResponse>> Delete(long id)
    {
        await _userService.DeleteAsync(id);
        return NoContent();
    }

    private ActionResult EmailConflict(EmailAlreadyExistsException ex)
    {
        LogEmailAlreadyInUse(ex.Email);
        ModelState.AddModelError(nameof(UserDto.Email), ex.Message);
        return ValidationProblem(ModelState);
    }

    // Handled errors, logged where they are handled so the reason survives the response. A missing id is a
    // client mistake and stays at Information; the two write failures are worth a Warning.
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "User {UserId} was not found")]
    private partial void LogUserNotFound(long userId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Email {Email} is already in use by another user")]
    private partial void LogEmailAlreadyInUse(string email);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "User {UserId} no longer exists; the update was not applied")]
    private partial void LogUserNoLongerExists(long userId);
}
