using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Refit;
using UserManagement.Api.Contracts.Auth;
using UserManagement.Blazor.Api;

namespace UserManagement.Blazor.Auth;

/// <summary>
/// The two form posts that change who the browser is signed in as. Both are genuine HTTP posts (not
/// circuit events) because only an HTTP response can set or clear the auth cookie, and both require the
/// antiforgery token so a third-party page cannot forge them.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/login", LoginAsync);
        // Cast to Delegate: with only HttpContext as a parameter the method group would otherwise bind to the
        // RequestDelegate overload, which discards the IResult instead of writing it to the response.
        endpoints.MapPost("/logout", (Delegate)LogoutAsync);
        return endpoints;
    }

    [RequireAntiforgeryToken]
    public static async Task<IResult> LoginAsync([FromForm] LoginRequest request, IAuthApi authApi, HttpContext httpContext)
    {
        if (!AntiforgeryValidationPassed(httpContext)) return AntiforgeryFailure();

        LoginResponse login;
        try
        {
            login = await authApi.LoginAsync(request);
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
        {
            return Results.Redirect("/login?error=1");
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, login.DisplayName),
            new Claim(ClaimTypes.Email, login.Email),
            new Claim(AuthClaimTypes.AccessToken, login.Token)
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { ExpiresUtc = login.ExpiresAtUtc });

        return Results.Redirect("/");
    }

    [RequireAntiforgeryToken]
    public static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        if (!AntiforgeryValidationPassed(httpContext)) return AntiforgeryFailure();

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }

    // The antiforgery middleware validates the token for [RequireAntiforgeryToken] endpoints and records the
    // outcome as a request feature, but only endpoints that bind form parameters turn a failed validation
    // into a 400 on their own. Logout binds nothing, so both handlers check the outcome themselves: a post
    // that was not validated (no form body, or a forged one) is rejected before it can touch the sign-in state.
    // A 400 with a body: the status-code-pages middleware re-executes body-less 4xx responses through the
    // NotFound page, which sits behind [Authorize] and would turn this into a redirect to the login page.
    private static IResult AntiforgeryFailure()
        => Results.Problem(title: "The request could not be verified as coming from this site.", statusCode: StatusCodes.Status400BadRequest);

    private static bool AntiforgeryValidationPassed(HttpContext httpContext)
        => httpContext.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: true };
}
