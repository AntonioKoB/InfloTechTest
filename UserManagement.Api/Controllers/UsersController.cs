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

    // The three writes are accepted here and executed by the worker. The command is marked Pending before it
    // is published: the worker can finish before this request returns, and a later Pending mark would
    // overwrite its outcome.

    [HttpPost]
    public async Task<ActionResult<CommandAcceptedResponse>> Create(CreateUserRequest request)
    {
        var user = request.ToUser();
        _credentialService.SetPassword(user, request.Password);
        var command = user.ToCreateCommand(Guid.NewGuid());

        await _statusStore.MarkPendingAsync(command.CommandId);
        await _messageBus.PublishAsync(command);
        return AcceptedAtAction(nameof(CommandsController.GetStatus), "Commands", new { id = command.CommandId }, new CommandAcceptedResponse { CommandId = command.CommandId });
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

        var command = user.ToUpdateCommand(Guid.NewGuid(), passwordHash);
        await _statusStore.MarkPendingAsync(command.CommandId);
        await _messageBus.PublishAsync(command);
        return AcceptedAtAction(nameof(CommandsController.GetStatus), "Commands", new { id = command.CommandId }, new CommandAcceptedResponse { CommandId = command.CommandId });
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<CommandAcceptedResponse>> Delete(long id)
    {
        var command = new DeleteUserCommand(Guid.NewGuid(), id);
        await _statusStore.MarkPendingAsync(command.CommandId);
        await _messageBus.PublishAsync(command);
        return AcceptedAtAction(nameof(CommandsController.GetStatus), "Commands", new { id = command.CommandId }, new CommandAcceptedResponse { CommandId = command.CommandId });
    }

    // Handled errors, logged where they are handled so the reason survives the response. A missing id is a
    // client mistake and stays at Information.
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "User {UserId} was not found")]
    private partial void LogUserNotFound(long userId);
}
