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
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _credentialService.AuthenticateAsync(request.Email, request.Password);
        if (user is null) return Unauthorized();

        var issued = _jwtTokenService.CreateToken(user);

        return Ok(new LoginResponse
        {
            Token = issued.Value,
            ExpiresAtUtc = issued.ExpiresAtUtc,
            DisplayName = $"{user.Forename} {user.Surname}",
            Email = user.Email
        });
    }
}
