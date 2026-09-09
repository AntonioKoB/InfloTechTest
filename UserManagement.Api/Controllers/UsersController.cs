using UserManagement.Api.Contracts.Logs;
using UserManagement.Api.Contracts.Users;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserLogService _userLogService;

    public UsersController(IUserService userService, IUserLogService userLogService)
    {
        _userService = userService;
        _userLogService = userLogService;
    }

    [HttpGet]
    public Task<ActionResult<IEnumerable<UserDto>>> GetUsers(UserListFilter filter = UserListFilter.All)
        => throw new NotImplementedException();

    [HttpGet("{id:long}")]
    public Task<ActionResult<UserDto>> GetById(long id, bool recordAsViewed = false)
        => throw new NotImplementedException();

    [HttpGet("{id:long}/logs")]
    public Task<ActionResult<IEnumerable<UserLogDto>>> GetUserLogs(long id)
        => throw new NotImplementedException();

    [HttpPost]
    public Task<ActionResult<UserDto>> Create(CreateUserRequest request)
        => throw new NotImplementedException();

    [HttpPut("{id:long}")]
    public Task<ActionResult<UserDto>> Update(long id, UpdateUserRequest request)
        => throw new NotImplementedException();

    [HttpDelete("{id:long}")]
    public Task<IActionResult> Delete(long id)
        => throw new NotImplementedException();
}
