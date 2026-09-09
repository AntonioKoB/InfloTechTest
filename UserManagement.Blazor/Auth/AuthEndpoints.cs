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
        endpoints.MapPost("/logout", LogoutAsync);
        return endpoints;
    }

    [RequireAntiforgeryToken]
    public static async Task<IResult> LoginAsync([FromForm] LoginRequest request, [FromForm] string? returnUrl, IAuthApi authApi, HttpContext httpContext)
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

        return Results.Redirect(LocalReturnUrlOrHome(returnUrl));
    }

    [RequireAntiforgeryToken]
    public static async Task<IResult> LogoutAsync(HttpContext httpContext, IAuthApi authApi)
    {
        if (!AntiforgeryValidationPassed(httpContext)) return AntiforgeryFailure();

        // Tell the API first so the sign-out is audited against the session's token. If the API already
        // rejects that token (expired), the local sign-out still goes ahead - the cookie is what keeps the
        // browser signed in, and it must go either way.
        var token = httpContext.User.FindFirst(AuthClaimTypes.AccessToken)?.Value;
        if (token is not null)
        {
            try
            {
                await authApi.LogoutAsync(token);
            }
            catch (ApiException)
            {
            }
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }

    // Only ever redirect within this site: an absolute or protocol-relative ReturnUrl could send a freshly
    // signed-in user anywhere.
    private static string LocalReturnUrlOrHome(string? returnUrl)
        => !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\")
            ? returnUrl
            : "/";

    // A 400 with a body: the status-code-pages middleware re-executes body-less 4xx responses through the
    // NotFound page, which sits behind [Authorize] and would turn this into a redirect to the login page.
    private static IResult AntiforgeryFailure()
        => Results.Problem(title: "The request could not be verified as coming from this site.", statusCode: StatusCodes.Status400BadRequest);

    // The antiforgery middleware validates the token for [RequireAntiforgeryToken] endpoints and records the
    // outcome as a request feature, but only endpoints that bind form parameters turn a failed validation
    // into a 400 on their own. Logout binds nothing, so both handlers check the outcome themselves: a post
    // that was not validated (no form body, or a forged one) is rejected before it can touch the sign-in state.
    private static bool AntiforgeryValidationPassed(HttpContext httpContext)
        => httpContext.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: true };
}
