using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using UserManagement.Api.Auth;
using UserManagement.Api.Contracts.Auth;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Api.Controllers;

[ApiController]
[Authorize]
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

    /// <summary>
    /// Ends the session identified by the bearer token. There is no body: the token says who is signing out.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = long.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        await _credentialService.SignOutAsync(userId);
        return NoContent();
    }
}
