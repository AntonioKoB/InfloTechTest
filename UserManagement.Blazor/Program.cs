using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor.Services;
using Refit;
using UserManagement.Blazor.Api;
using UserManagement.Blazor.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

var refitSettings = new RefitSettings
{
    ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    })
};

var apiBaseUrl = new Uri(builder.Configuration["Api:BaseUrl"]!);

builder.Services.AddHttpClient(nameof(IUsersApi))
    .AddRefitClient<IUsersApi>(refitSettings)
    .ConfigureHttpClient(c => c.BaseAddress = apiBaseUrl)
    .AddStandardResilienceHandler();

builder.Services.AddHttpClient(nameof(ILogsApi))
    .AddRefitClient<ILogsApi>(refitSettings)
    .ConfigureHttpClient(c => c.BaseAddress = apiBaseUrl)
    .AddStandardResilienceHandler();

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
