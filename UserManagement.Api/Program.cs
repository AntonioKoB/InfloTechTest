using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using UserManagement.Api.Auth;
using UserManagement.Api.Caching;
using UserManagement.Api.Commands;
using UserManagement.Api.Health;
using UserManagement.Api.Telemetry;
using UserManagement.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDataAccess(builder.Configuration)
    .AddDomainServices()
    .AddJwtAuthentication(builder.Configuration, builder.Environment)
    .AddApiHealthChecks()
    .AddApiOutputCaching()
    .AddCommandWorker()
    .AddApiTelemetry(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<DataContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHsts();
app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapControllers();

// Anonymous on purpose: health probes carry no token. See HealthCheckExtensions for what it checks.
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
