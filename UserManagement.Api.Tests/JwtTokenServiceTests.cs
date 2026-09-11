using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using UserManagement.Api.Auth;
using UserManagement.Models;

namespace UserManagement.Api.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public async Task CreateToken_MustValidateWithTheConfiguredIssuerAudienceAndSigningKey()
    {
        // Arrange
        var service = CreateService();
        var user = CreateUser();

        // Act
        var issued = service.CreateToken(user);

        // Assert
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(issued.Value, ValidationParametersFor(SigningKey));
        result.IsValid.Should().BeTrue(result.Exception?.Message);
    }

    [Fact]
    public async Task CreateToken_MustNotValidateWithADifferentSigningKey()
    {
        // Arrange
        var service = CreateService();
        var user = CreateUser();

        // Act
        var issued = service.CreateToken(user);

        // Assert
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(issued.Value, ValidationParametersFor("a-completely-different-key-that-is-also-32-bytes"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateToken_MustCarryTheUserIdEmailAndNameAsClaims()
    {
        // Arrange
        var service = CreateService();
        var user = CreateUser();

        // Act
        var issued = service.CreateToken(user);

        // Assert
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(issued.Value);
        jwt.Subject.Should().Be(user.Id.ToString());
        jwt.GetClaim(JwtRegisteredClaimNames.Email).Value.Should().Be(user.Email);
        jwt.GetClaim(JwtRegisteredClaimNames.Name).Value.Should().Be($"{user.Forename} {user.Surname}");
    }

    [Fact]
    public void CreateToken_MustExpireAfterTheConfiguredNumberOfMinutes()
    {
        // Arrange
        var service = CreateService(expiryMinutes: 30);
        var user = CreateUser();

        // Act
        var issued = service.CreateToken(user);

        // Assert
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(issued.Value);
        issued.ExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(10));
        jwt.ValidTo.Should().BeCloseTo(issued.ExpiresAtUtc, TimeSpan.FromSeconds(1));
    }

    private const string Issuer = "UserManagement.Api.Tests";
    private const string Audience = "UserManagement.Blazor.Tests";
    private const string SigningKey = "unit-test-signing-key-that-is-at-least-32-bytes-long";

    private static TokenValidationParameters ValidationParametersFor(string key) => new()
    {
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ClockSkew = TimeSpan.Zero
    };

    private static User CreateUser() => new()
    {
        Id = 7,
        Forename = "Johnny",
        Surname = "User",
        Email = "juser@example.com",
        IsActive = true,
        DateOfBirth = new DateOnly(1990, 1, 1)
    };

    private static JwtTokenService CreateService(int expiryMinutes = 60)
        => new(Options.Create(new JwtOptions { Issuer = Issuer, Audience = Audience, SigningKey = SigningKey, ExpiryMinutes = expiryMinutes }));
}
