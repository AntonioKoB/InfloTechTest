using System;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Commands;
using UserManagement.Services.Domain.Exceptions;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Data.Tests;

public class UpdateUserCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_MustApplyTheCommandFieldsToTheExistingUserAndSaveIt()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        var existing = SetupExistingUser();
        var command = NewCommand();

        // Act: Invokes the method under test with the arranged parameters.
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _userService.Verify(s => s.UpdateAsync(existing), Times.Once);
        existing.Forename.Should().Be(command.Forename);
        existing.Surname.Should().Be(command.Surname);
        existing.Email.Should().Be(command.Email);
        existing.DateOfBirth.Should().Be(command.DateOfBirth);
        existing.IsActive.Should().Be(command.IsActive);
    }

    [Fact]
    public async Task HandleAsync_WhenAPasswordHashIsSupplied_MustReplaceTheExistingOne()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        var existing = SetupExistingUser();

        // Act: Invokes the method under test with the arranged parameters.
        await handler.HandleAsync(NewCommand(passwordHash: "new-hash"), CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        existing.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public async Task HandleAsync_WhenNoPasswordHashIsSupplied_MustKeepTheExistingOne()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        var existing = SetupExistingUser();

        // Act: Invokes the method under test with the arranged parameters.
        await handler.HandleAsync(NewCommand(passwordHash: null), CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        existing.PasswordHash.Should().Be("old-hash");
    }

    [Fact]
    public async Task HandleAsync_MustRecordUpdatedLogWithBeforeAndAfterSnapshots()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        var existing = SetupExistingUser();
        var command = NewCommand();

        // Act: Invokes the method under test with the arranged parameters.
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _userLogService.Verify(s => s.RecordAsync(
            5,
            UserLogAction.Updated,
            It.Is<User>(before => !ReferenceEquals(before, existing) && before.Forename == "Existing" && before.Email == "existing@example.com"),
            existing), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenTheUserNoLongerExists_MustThrowWithoutSavingOrRecording()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        _userService.Setup(s => s.GetByIdAsync(5, false)).ReturnsAsync((User?)null);

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().ThrowAsync<UserNoLongerExistsException>();
        _userService.Verify(s => s.UpdateAsync(It.IsAny<User>()), Times.Never);
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenUpdateAsyncThrows_MustRecordNothingAndLetTheExceptionPropagate()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        SetupExistingUser();
        var command = NewCommand();
        _userService.Setup(s => s.UpdateAsync(It.IsAny<User>())).ThrowsAsync(new EmailAlreadyExistsException(command.Email));

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => handler.HandleAsync(command, CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_MustReturnTheUserId()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        SetupExistingUser();

        // Act: Invokes the method under test with the arranged parameters.
        var userId = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        userId.Should().Be(5);
    }

    private User SetupExistingUser()
    {
        var existing = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1), IsActive = true, PasswordHash = "old-hash" };
        _userService.Setup(s => s.GetByIdAsync(5, false)).ReturnsAsync(existing);
        return existing;
    }

    private static UpdateUserCommand NewCommand(string? passwordHash = null)
        => new(Guid.NewGuid(), 5, "Updated", "Person", "updated@example.com", new DateOnly(1991, 2, 3), IsActive: false, PasswordHash: passwordHash);

    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IUserLogService> _userLogService = new();
    private UpdateUserCommandHandler CreateHandler() => new(_userService.Object, _userLogService.Object);
}
