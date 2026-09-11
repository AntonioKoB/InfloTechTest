using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using UserManagement.Api.Auth;
using UserManagement.Api.Contracts.Auth;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public partial class AuthController : ControllerBase
{
    private readonly ICredentialService _credentialService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ICredentialService credentialService, IJwtTokenService jwtTokenService, ILogger<AuthController> logger)
    {
        _credentialService = credentialService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _credentialService.AuthenticateAsync(request.Email, request.Password);
        if (user is null)
        {
            LogLoginRejected(request.Email);
            return Unauthorized();
        }

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
    /// Ends the session identified by the bearer token.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = long.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        await _credentialService.SignOutAsync(userId);
        return NoContent();
    }

    // The email is logged, never the password. Repeated failures for one email are what a guessing attempt
    // looks like, hence Warning.
    [LoggerMessage(EventId = 1101, Level = LogLevel.Warning, Message = "Login rejected for {Email}")]
    private partial void LogLoginRejected(string email);
}
