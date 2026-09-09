using System;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Domain.Implementations;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Data.Tests;

public class AuditingCredentialServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_WhenTheInnerServiceReturnsAUser_MustRecordALoggedInEntryForThatUser()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = CreateUser();
        _inner.Setup(s => s.AuthenticateAsync(user.Email, "12345")).ReturnsAsync(user);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.AuthenticateAsync(user.Email, "12345");

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeSameAs(user);
        _userLogService.Verify(s => s.RecordAsync(user.Id, UserLogAction.LoggedIn, null, null), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenTheInnerServiceRejectsTheCredentials_MustRecordNothing()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // A failed attempt has no verified user to attribute it to, so it leaves no audit entry.
        var service = CreateService();
        _inner.Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.AuthenticateAsync("nobody@example.com", "wrong");

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeNull();
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    [Fact]
    public async Task SignOutAsync_MustSignOutThroughTheInnerServiceAndRecordALoggedOutEntry()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();

        // Act: Invokes the method under test with the arranged parameters.
        await service.SignOutAsync(7);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.SignOutAsync(7), Times.Once);
        _userLogService.Verify(s => s.RecordAsync(7, UserLogAction.LoggedOut, null, null), Times.Once);
    }

    [Fact]
    public void SetPassword_MustPassStraightThroughWithoutRecordingAnything()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Setting a password is part of creating/updating a user, which AuditingUserService already records.
        var service = CreateService();
        var user = CreateUser();

        // Act: Invokes the method under test with the arranged parameters.
        service.SetPassword(user, "12345");

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.SetPassword(user, "12345"), Times.Once);
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    private static User CreateUser() => new()
    {
        Id = 7,
        Forename = "Johnny",
        Surname = "User",
        Email = "juser@example.com",
        IsActive = true,
        DateOfBirth = new DateOnly(1990, 1, 1)
    };

    private readonly Mock<ICredentialService> _inner = new();
    private readonly Mock<IUserLogService> _userLogService = new();
    private AuditingCredentialService CreateService() => new(_inner.Object, _userLogService.Object);
}
