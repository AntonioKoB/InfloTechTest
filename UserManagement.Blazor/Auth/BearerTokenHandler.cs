using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace UserManagement.Blazor.Auth;

/// <summary>
/// Adds the signed-in user's bearer token to API calls. A 401 means the token has expired, so reload to the
/// login page.
/// </summary>
public partial class BearerTokenHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<BearerTokenHandler> _logger;

    public BearerTokenHandler(AuthenticationStateProvider authenticationStateProvider, NavigationManager navigationManager, ILogger<BearerTokenHandler> logger)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _navigationManager = navigationManager;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var token = state.User.FindFirst(AuthClaimTypes.AccessToken)?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            LogTokenRejected(request.Method, request.RequestUri?.AbsolutePath);
            _navigationManager.NavigateTo("/login", forceLoad: true);
        }

        return response;
    }

    [LoggerMessage(EventId = 2101, Level = LogLevel.Warning, Message = "The API rejected the session token for {Method} {Path}; redirecting to login")]
    private partial void LogTokenRejected(HttpMethod method, string? path);
}
