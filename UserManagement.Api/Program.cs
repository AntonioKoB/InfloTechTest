using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using UserManagement.Api.Auth;
using UserManagement.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDataAccess(builder.Configuration)
    .AddDomainServices();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
{
    // Fail at startup rather than at the first login: a missing key would otherwise surface as an opaque
    // 500 from /api/auth/login. The key is a secret and lives in user-secrets / the environment, like the
    // connection string - see the README's Authentication section.
    throw new InvalidOperationException(
        $"Jwt:SigningKey is missing or shorter than 32 bytes (environment: {builder.Environment.EnvironmentName}; " +
        "user-secrets are only loaded in Development). From the UserManagement.Api folder run: " +
        "dotnet user-secrets set \"Jwt:SigningKey\" \"<a random string of at least 32 characters>\"");
}

builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
    });
builder.Services.AddAuthorization();

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

app.MapControllers();

app.Run();
