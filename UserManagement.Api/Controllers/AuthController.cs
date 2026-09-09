using UserManagement.Api.Auth;
using UserManagement.Api.Contracts.Auth;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ICredentialService _credentialService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(ICredentialService credentialService, IJwtTokenService jwtTokenService)
    {
        _credentialService = credentialService;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    public Task<ActionResult<LoginResponse>> Login(LoginRequest request) => throw new NotImplementedException();
}
