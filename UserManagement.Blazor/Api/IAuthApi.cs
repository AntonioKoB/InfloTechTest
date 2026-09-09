using Refit;
using UserManagement.Api.Contracts.Auth;

namespace UserManagement.Blazor.Api;

public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<LoginResponse> LoginAsync([Body] LoginRequest request);

    /// <summary>
    /// Ends the session identified by the given token, which Refit sends as the bearer Authorization header.
    /// </summary>
    [Post("/api/auth/logout")]
    Task LogoutAsync([Authorize("Bearer")] string token);
}
