using System.Threading;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using UserManagement.Api.Caching;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Api.Contracts.Users;
using UserManagement.Api.Mapping;
using UserManagement.Services.Domain.Exceptions;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public partial class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserLogService _userLogService;
    private readonly ICredentialService _credentialService;
    private readonly IOutputCacheStore _outputCache;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, IUserLogService userLogService, ICredentialService credentialService, IOutputCacheStore outputCache, ILogger<UsersController> logger)
    {
        _userService = userService;
        _userLogService = userLogService;
        _credentialService = credentialService;
        _outputCache = outputCache;
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

        await EvictUsersListAsync();
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user.ToDto());
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<UserDto>> Update(long id, UpdateUserRequest request)
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

        await EvictUsersListAsync();
        return Ok(user.ToDto());
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _userService.DeleteAsync(id);
        await EvictUsersListAsync();
        return NoContent();
    }

    private ActionResult EmailConflict(EmailAlreadyExistsException ex)
    {
        LogEmailAlreadyInUse(ex.Email);
        ModelState.AddModelError(nameof(UserDto.Email), ex.Message);
        return ValidationProblem(ModelState);
    }

    // Called after a write has succeeded. The cached list must go even if the caller has disconnected by now,
    // so this deliberately ignores the request's cancellation token. The single-user cache is invalidated by
    // the service layer, where that read is cached.
    private Task EvictUsersListAsync()
        => _outputCache.EvictByTagAsync(OutputCachingExtensions.UsersTag, CancellationToken.None).AsTask();

    // Handled errors, logged where they are handled so the reason survives the response. A missing id is a
    // client mistake and stays at Information; the two write failures are worth a Warning.
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "User {UserId} was not found")]
    private partial void LogUserNotFound(long userId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Email {Email} is already in use by another user")]
    private partial void LogEmailAlreadyInUse(string email);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "User {UserId} no longer exists; the update was not applied")]
    private partial void LogUserNoLongerExists(long userId);
}
