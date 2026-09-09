using System.Net.Http;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace UserManagement.Blazor.Auth;

public class BearerTokenHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly NavigationManager _navigationManager;

    public BearerTokenHandler(AuthenticationStateProvider authenticationStateProvider, NavigationManager navigationManager)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _navigationManager = navigationManager;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
