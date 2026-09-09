using Refit;
using UserManagement.Api.Contracts.Auth;

namespace UserManagement.Blazor.Api;

public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<LoginResponse> LoginAsync([Body] LoginRequest request);
}
