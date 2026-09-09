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
    /// Token issuance plus bearer validation, both driven by the Jwt configuration section. Fails at startup
    /// when the signing key is unusable: a missing key would otherwise surface as an opaque 500 from the
    /// first login. The key is a secret and lives in user-secrets / the environment, like the connection
    /// string - see the README's Authentication section.
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
                // Keep the JWT claim names (sub, email, name) rather than remapping them to the legacy XML
                // schema names, so controllers read exactly what JwtTokenService wrote.
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
