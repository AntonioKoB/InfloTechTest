using Refit;
using UserManagement.Api.Contracts.Auth;

namespace UserManagement.Blazor.Api;

public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<LoginResponse> LoginAsync([Body] LoginRequest request);

    /// <summary>
    /// Ends the session; Refit sends the token as the bearer header.
    /// </summary>
    [Post("/api/auth/logout")]
    Task LogoutAsync([Authorize("Bearer")] string token);
}
