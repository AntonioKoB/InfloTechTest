using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Api.Contracts.Auth;
using UserManagement.Blazor.Api;

namespace UserManagement.Blazor.Auth;

public static class AuthEndpoints
{
    public static Task<IResult> LoginAsync([FromForm] LoginRequest request, IAuthApi authApi, HttpContext httpContext)
        => throw new NotImplementedException();

    public static Task<IResult> LogoutAsync(HttpContext httpContext)
        => throw new NotImplementedException();
}
