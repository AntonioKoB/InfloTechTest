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

// The cookie's principal carries the API bearer token as a claim, so the token never reaches browser script.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        // Expiry matches the token's lifetime and is never extended past it.
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

builder.Services.AddHttpClient(nameof(IAuthApi))
    .AddRefitClient<IAuthApi>(refitSettings)
    .ConfigureHttpClient(c => c.BaseAddress = apiBaseUrl)
    .AddStandardResilienceHandler();

// Built per circuit rather than through AddRefitClient: IHttpClientFactory builds handler pipelines outside
// the circuit's DI scope, so a handler there cannot see the authentication state. The named clients still
// supply the resilience pipeline.
builder.Services.AddHttpClient(nameof(IUsersApi)).AddStandardResilienceHandler();
builder.Services.AddHttpClient(nameof(ILogsApi)).AddStandardResilienceHandler();
builder.Services.AddScoped(sp => CreateAuthenticatedClient<IUsersApi>(sp));
builder.Services.AddScoped(sp => CreateAuthenticatedClient<ILogsApi>(sp));

builder.Services.AddScoped<ICommandPoller>(sp => new CommandPoller(sp.GetRequiredService<IUsersApi>(), TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(30)));

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

// Liveness only; the API's /health covers the database. Anonymous: probes carry no token.
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
