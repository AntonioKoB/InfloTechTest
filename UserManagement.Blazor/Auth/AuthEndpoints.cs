using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Refit;
using UserManagement.Api.Contracts.Auth;
using UserManagement.Blazor.Api;

namespace UserManagement.Blazor.Auth;

/// <summary>
/// The login and logout form posts. Real HTTP posts rather than circuit events, because only an HTTP response
/// can set or clear the auth cookie; both require the antiforgery token.
/// </summary>
public static partial class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // The /login page answers POST too. Declaring the form content types is what makes routing pick this
        // handler for a form post; without it every POST /login is an AmbiguousMatchException.
        endpoints.MapPost("/login", LoginAsync).Accepts<LoginRequest>("application/x-www-form-urlencoded", "multipart/form-data");
        endpoints.MapPost("/logout", LogoutAsync);
        return endpoints;
    }

    [RequireAntiforgeryToken]
    public static async Task<IResult> LoginAsync(HttpContext httpContext, IAuthApi authApi, ILoggerFactory loggerFactory)
    {
        var logger = CreateLogger(loggerFactory);
        if (!AntiforgeryValidationPassed(httpContext))
        {
            LogAntiforgeryValidationFailed(logger, "login");
            return AntiforgeryFailure();
        }

        // Only after the check above: reading the form while antiforgery validation stands failed throws.
        var form = await httpContext.Request.ReadFormAsync();
        var request = new LoginRequest { Email = form["Email"].ToString(), Password = form["Password"].ToString() };
        string? returnUrl = form["ReturnUrl"];

        LoginResponse login;
        try
        {
            login = await authApi.LoginAsync(request);
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
        {
            LogLoginRejected(logger, request.Email, ex.StatusCode);
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
    public static async Task<IResult> LogoutAsync(HttpContext httpContext, IAuthApi authApi, ILoggerFactory loggerFactory)
    {
        var logger = CreateLogger(loggerFactory);
        if (!AntiforgeryValidationPassed(httpContext))
        {
            LogAntiforgeryValidationFailed(logger, "logout");
            return AntiforgeryFailure();
        }

        // Tell the API first so the sign-out is audited; sign out locally even if the API rejects an expired
        // token.
        var token = httpContext.User.FindFirst(AuthClaimTypes.AccessToken)?.Value;
        if (token is not null)
        {
            try
            {
                await authApi.LogoutAsync(token);
            }
            catch (ApiException ex)
            {
                LogApiLogoutFailed(logger, ex.StatusCode);
            }
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }

    // Only redirect within this site.
    private static string LocalReturnUrlOrHome(string? returnUrl)
        => !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\")
            ? returnUrl
            : "/";

    // A body-less 400 would be re-executed through the NotFound page, which sits behind [Authorize].
    private static IResult AntiforgeryFailure()
        => Results.Problem(title: "The request could not be verified as coming from this site.", statusCode: StatusCodes.Status400BadRequest);

    // Reading the form after a failed antiforgery validation throws, so both handlers check the recorded
    // outcome before touching the form. [FromForm] parameters would read it during binding, before the
    // handler runs.
    private static bool AntiforgeryValidationPassed(HttpContext httpContext)
        => httpContext.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: true };

    // A static class cannot be a type argument for ILogger<T>.
    private static ILogger CreateLogger(ILoggerFactory loggerFactory) => loggerFactory.CreateLogger(typeof(AuthEndpoints));

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "Login rejected by the API for {Email} ({StatusCode})")]
    private static partial void LogLoginRejected(ILogger logger, string email, HttpStatusCode statusCode);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = "Antiforgery validation failed for the {Endpoint} post")]
    private static partial void LogAntiforgeryValidationFailed(ILogger logger, string endpoint);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "The API rejected the logout ({StatusCode}); signing out locally anyway")]
    private static partial void LogApiLogoutFailed(ILogger logger, HttpStatusCode statusCode);
}
