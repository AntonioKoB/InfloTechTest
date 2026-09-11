using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace UserManagement.Api.Auth;

public static class JwtAuthenticationExtensions
{
    private const int MinimumSigningKeyBytes = 32;

    /// <summary>
    /// Token issuance and bearer validation from the Jwt configuration section. Fails at startup if the
    /// signing key is missing or too short; the key lives in user-secrets or the environment.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection(JwtOptions.SectionName);
        var options = section.Get<JwtOptions>() ?? new JwtOptions();
        if (Encoding.UTF8.GetByteCount(options.SigningKey) < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey is missing or shorter than {MinimumSigningKeyBytes} bytes (environment: {environment.EnvironmentName}; " +
                "user-secrets are only loaded in Development). From the UserManagement.Api folder run: " +
                "dotnet user-secrets set \"Jwt:SigningKey\" \"<a random string of at least 32 characters>\"");
        }

        services.Configure<JwtOptions>(section);
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearer =>
            {
                // Keep the sub/email/name claim names; controllers read what JwtTokenService wrote.
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    NameClaimType = JwtRegisteredClaimNames.Name
                };
            });
        services.AddAuthorization();

        return services;
    }
}
