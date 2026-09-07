using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Domain.Exceptions;
using UserManagement.Services.Domain.Implementations;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Data.Tests;

public class AuditingUserServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenCalled_MustRecordCreatedLogWithAfterSnapshot()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = new User { Id = 5, Forename = "Brand New", Surname = "User", Email = "brandnewuser@example.com", DateOfBirth = new DateOnly(1995, 4, 12) };

        // Act: Invokes the method under test with the arranged parameters.
        await service.CreateAsync(user);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.CreateAsync(user), Times.Once);
        _userLogService.Verify(s => s.RecordAsync(user.Id, UserLogAction.Created, null, user), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenInnerThrows_MustNotRecordLog()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = new User { Forename = "Brand New", Surname = "User", Email = "taken@example.com", DateOfBirth = new DateOnly(1995, 4, 12) };
        _inner.Setup(s => s.CreateAsync(user)).ThrowsAsync(new EmailAlreadyExistsException(user.Email));

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => service.CreateAsync(user);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenCalled_MustRecordUpdatedLogWithBeforeAndAfterSnapshots()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var before = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        var after = new User { Id = 5, Forename = "Updated", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        _dataContext
            .Setup(s => s.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(before);

        // Act: Invokes the method under test with the arranged parameters.
        await service.UpdateAsync(after);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.UpdateAsync(after), Times.Once);
        _userLogService.Verify(s => s.RecordAsync(after.Id, UserLogAction.Updated, before, after), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenInnerThrows_MustNotRecordLog()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var user = new User { Id = 5, Forename = "Updated", Surname = "User", Email = "taken@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        _inner.Setup(s => s.UpdateAsync(user)).ThrowsAsync(new EmailAlreadyExistsException(user.Email));

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => service.UpdateAsync(user);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserExists_MustRecordDeletedLogWithBeforeSnapshot()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var before = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        _dataContext
            .Setup(s => s.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(before);

        // Act: Invokes the method under test with the arranged parameters.
        await service.DeleteAsync(5);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.DeleteAsync(5), Times.Once);
        _userLogService.Verify(s => s.RecordAsync(5, UserLogAction.Deleted, before, null), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserDoesNotExist_MustNotRecordAnyLog()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Matches DeleteAsync's own idempotent-delete behaviour (deleting an already-gone user does not
        // error) - there is nothing meaningful to audit for a no-op delete.
        var service = CreateService();
        _dataContext
            .Setup(s => s.FirstOrDefaultAsync<User>(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);

        // Act: Invokes the method under test with the arranged parameters.
        await service.DeleteAsync(999);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _inner.Verify(s => s.DeleteAsync(999), Times.Once);
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRecordAsViewedIsFalse_MustNotRecordAnyLog()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Deliberate design decision: GetByIdAsync is shared by the View screen, Edit's form pre-fill, Edit's
        // own re-fetch before saving, and Delete's confirmation screen - only the caller knows which of these
        // it is, so it says so via recordAsViewed rather than the decorator guessing from context. Callers
        // other than the View screen pass the default (false), so no log is recorded here.
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
    private readonly Mock<IDataContext> _dataContext = new();
    private AuditingUserService CreateService() => new(_inner.Object, _userLogService.Object, _dataContext.Object);
}
