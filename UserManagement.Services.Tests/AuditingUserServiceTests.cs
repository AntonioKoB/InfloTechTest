using System;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Domain.Implementations;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Data.Tests;

public class AuditingUserServiceTests
{
    [Fact]
    public async Task CreateAsync_MustOnlyDelegate_TheCreatedLogIsRecordedByTheCommandHandler()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = new User { Id = 5, Forename = "Brand New", Surname = "User", Email = "brandnewuser@example.com", DateOfBirth = new DateOnly(1995, 4, 12) };

        // Act: Invokes the method under test with the arranged parameters.
        await service.CreateAsync(user);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.CreateAsync(user), Times.Once);
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MustOnlyDelegate_TheUpdatedLogIsRecordedByTheCommandHandler()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = new User { Id = 5, Forename = "Updated", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };

        // Act: Invokes the method under test with the arranged parameters.
        await service.UpdateAsync(user);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.UpdateAsync(user), Times.Once);
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_MustOnlyDelegate_TheDeletedLogIsRecordedByTheCommandHandler()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();

        // Act: Invokes the method under test with the arranged parameters.
        await service.DeleteAsync(5);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.DeleteAsync(5), Times.Once);
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRecordAsViewedIsFalse_MustNotRecordAnyLog()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        _inner.Setup(s => s.GetByIdAsync(5, false)).ReturnsAsync(user);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetByIdAsync(5);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeSameAs(user);
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRecordAsViewedIsTrue_MustRecordViewedLog()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        _inner.Setup(s => s.GetByIdAsync(5, true)).ReturnsAsync(user);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetByIdAsync(5, recordAsViewed: true);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeSameAs(user);
        _userLogService.Verify(s => s.RecordAsync(5, UserLogAction.Viewed, null, user), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRecordAsViewedIsTrueButUserDoesNotExist_MustNotRecordAnyLog()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        _inner.Setup(s => s.GetByIdAsync(999, true)).ReturnsAsync((User?)null);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetByIdAsync(999, recordAsViewed: true);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeNull();
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    private readonly Mock<IUserService> _inner = new();
    private readonly Mock<IUserLogService> _userLogService = new();
    private AuditingUserService CreateService() => new(_inner.Object, _userLogService.Object);
}
