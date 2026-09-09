using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using UserManagement.Models;
using UserManagement.Services.Domain.Implementations;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Data.Tests;

public class CredentialServiceTests
{
    [Fact]
    public void SetPassword_MustStoreAHashThatIsNotThePlainTextPassword()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = CreateUser();

        // Act: Invokes the method under test with the arranged parameters.
        service.SetPassword(user, "12345");

        // Assert: Verifies that the action of the method under test behaves as expected.
        user.PasswordHash.Should().NotBeNullOrEmpty();
        user.PasswordHash.Should().NotBe("12345");
    }

    [Fact]
    public void SetPassword_MustStoreAHashThatVerifiesAgainstTheOriginalPassword()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = CreateUser();

        // Act: Invokes the method under test with the arranged parameters.
        service.SetPassword(user, "12345");

        // Assert: Verifies that the action of the method under test behaves as expected.
        _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, "12345").Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenEmailAndPasswordMatchAnActiveUser_MustReturnThatUser()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = SetupUser(password: "12345");

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.AuthenticateAsync(user.Email, "12345");

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeSameAs(user);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenPasswordIsWrong_MustReturnNull()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = SetupUser(password: "12345");

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.AuthenticateAsync(user.Email, "not-the-password");

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenEmailIsUnknown_MustReturnNull()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        _userService.Setup(s => s.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.AuthenticateAsync("nobody@example.com", "12345");

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserIsNotActive_MustReturnNullEvenWithTheRightPassword()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // IsActive is the app's own notion of a disabled account - a deactivated user keeps their row (and
        // their history) but must not be able to sign in.
        var service = CreateService();
        var user = SetupUser(password: "12345", isActive: false);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.AuthenticateAsync(user.Email, "12345");

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeNull();
    }

    private User SetupUser(string password, bool isActive = true)
    {
        var user = CreateUser(isActive);
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _userService
            .Setup(s => s.GetByEmailAsync(user.Email))
            .ReturnsAsync(user);

        return user;
    }

    private static User CreateUser(bool isActive = true) => new()
    {
        Id = 1,
        Forename = "Johnny",
        Surname = "User",
        Email = "juser@example.com",
        IsActive = isActive,
        DateOfBirth = new System.DateOnly(1990, 1, 1)
    };

    private readonly Mock<IUserService> _userService = new();
    private readonly PasswordHasher<User> _passwordHasher = new();
    private CredentialService CreateService() => new(_userService.Object, _passwordHasher);
}
