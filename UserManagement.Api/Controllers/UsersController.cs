using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using UserManagement.Api.Caching;
using UserManagement.Api.Contracts.Commands;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Api.Contracts.Users;
using UserManagement.Api.Mapping;
using UserManagement.Services.Commands;
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
    private readonly IMessageBus _messageBus;
    private readonly ICommandStatusStore _statusStore;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, IUserLogService userLogService, ICredentialService credentialService, IMessageBus messageBus, ICommandStatusStore statusStore, ILogger<UsersController> logger)
    {
        _userService = userService;
        _userLogService = userLogService;
        _credentialService = credentialService;
        _messageBus = messageBus;
        _statusStore = statusStore;
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
        return await AcceptAsync(user.ToCreateCommand(Guid.NewGuid()));
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
        string? passwordHash = null;
        if (!string.IsNullOrEmpty(request.Password))
        {
            _credentialService.SetPassword(user, request.Password);
            passwordHash = user.PasswordHash;
        }

        return await AcceptAsync(user.ToUpdateCommand(Guid.NewGuid(), passwordHash));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<CommandAcceptedResponse>> Delete(long id)
        => await AcceptAsync(new DeleteUserCommand(Guid.NewGuid(), id));

    // Mark Pending before publishing: the worker can finish before this request returns.
    private async Task<ActionResult<CommandAcceptedResponse>> AcceptAsync(ICommand command)
    {
        await _statusStore.MarkPendingAsync(command.CommandId);
        await _messageBus.PublishAsync(command);
        return AcceptedAtAction(nameof(CommandsController.GetStatus), "Commands", new { id = command.CommandId }, new CommandAcceptedResponse { CommandId = command.CommandId });
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "User {UserId} was not found")]
    private partial void LogUserNotFound(long userId);
}
