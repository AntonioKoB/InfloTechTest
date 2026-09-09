using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace UserManagement.Blazor.Auth;

/// <summary>
/// Attaches the signed-in user's API bearer token to every outgoing API call. A 401 from the API means the
/// token is no longer accepted (typically expired); the cookie expires with it, so a full page load lands
/// the user on the login page with clean state instead of a stale circuit.
/// </summary>
public class BearerTokenHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly NavigationManager _navigationManager;

    public BearerTokenHandler(AuthenticationStateProvider authenticationStateProvider, NavigationManager navigationManager)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _navigationManager = navigationManager;
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
            _navigationManager.NavigateTo("/login", forceLoad: true);
        }

        return response;
    }
}
