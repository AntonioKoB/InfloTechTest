using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Refit;
using UserManagement.Blazor.Api;
using UserManagement.Blazor.Auth;
using UserManagement.Blazor.Components;
using UserManagement.Blazor.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// The browser is signed in with a cookie issued by this app once the API has accepted the credentials. The
// cookie's principal carries the API bearer token as a claim, so the token never reaches browser script.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        // The expiry is set per sign-in to match the token's own lifetime and must not be extended past it.
        options.SlidingExpiration = false;
    });
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddBlazorTelemetry(builder.Configuration);
builder.Services.AddCascadingAuthenticationState();

var refitSettings = new RefitSettings
{
    ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    })
};

var apiBaseUrl = new Uri(builder.Configuration["Api:BaseUrl"]!);

// Login carries no token - the credentials are the request body.
builder.Services.AddHttpClient(nameof(IAuthApi))
    .AddRefitClient<IAuthApi>(refitSettings)
    .ConfigureHttpClient(c => c.BaseAddress = apiBaseUrl)
    .AddStandardResilienceHandler();

// Everything else sends the signed-in user's bearer token. These clients are built per circuit (scoped)
// rather than through AddRefitClient: IHttpClientFactory builds handler pipelines in its own DI scope, so a
// handler registered there could never see the circuit's authentication state. The named clients still
// supply the resilience pipeline; the bearer handler wraps it from the outside so retries re-send the header.
builder.Services.AddHttpClient(nameof(IUsersApi)).AddStandardResilienceHandler();
builder.Services.AddHttpClient(nameof(ILogsApi)).AddStandardResilienceHandler();
builder.Services.AddScoped(sp => CreateAuthenticatedClient<IUsersApi>(sp));
builder.Services.AddScoped(sp => CreateAuthenticatedClient<ILogsApi>(sp));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapAuthEndpoints();

// Liveness only - this host has no database of its own; the API's /health covers that. Anonymous on
// purpose: probes carry no token.
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

T CreateAuthenticatedClient<T>(IServiceProvider services) where T : class
{
    var handler = new BearerTokenHandler(
        services.GetRequiredService<AuthenticationStateProvider>(),
        services.GetRequiredService<NavigationManager>(),
        services.GetRequiredService<ILogger<BearerTokenHandler>>())
    {
        InnerHandler = services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(typeof(T).Name)
    };

    return RestService.For<T>(new HttpClient(handler) { BaseAddress = apiBaseUrl }, refitSettings);
}
