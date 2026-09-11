using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Commands;
using UserManagement.Services.Domain.Exceptions;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Data.Tests;

/// <summary>
/// The create handler is where the user write happens now that the API only accepts. It writes through
/// IUserService (uniqueness check, caching) and records the Created entry the way the auditing decorator
/// used to, so the log carries the same snapshot.
/// </summary>
public class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_MustCreateAUserFromTheCommandFieldsIncludingThePasswordHash()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        var command = NewCommand();

        // Act: Invokes the method under test with the arranged parameters.
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _userService.Verify(s => s.CreateAsync(It.Is<User>(u =>
            u.Forename == command.Forename &&
            u.Surname == command.Surname &&
            u.Email == command.Email &&
            u.DateOfBirth == command.DateOfBirth &&
            u.IsActive == command.IsActive &&
            u.PasswordHash == command.PasswordHash)), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_MustRecordACreatedLogWithTheSavedUserAsTheAfterSnapshot()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // The id exists only after the save, so the log must be recorded after CreateAsync, not before.
        var handler = CreateHandler();
        var command = NewCommand();
        _userService.Setup(s => s.CreateAsync(It.IsAny<User>())).Callback<User>(u => u.Id = 42).Returns(Task.CompletedTask);
        var recordedAfterSave = false;
        _userLogService
            .Setup(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()))
            .Callback(() => recordedAfterSave = _userService.Invocations.Any(i => i.Method.Name == nameof(IUserService.CreateAsync)))
            .Returns(Task.CompletedTask);

        // Act: Invokes the method under test with the arranged parameters.
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _userLogService.Verify(s => s.RecordAsync(42, UserLogAction.Created, null, It.Is<User>(u => u.Id == 42 && u.Email == command.Email)), Times.Once);
        recordedAfterSave.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_MustReturnTheNewUserId()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        _userService.Setup(s => s.CreateAsync(It.IsAny<User>())).Callback<User>(u => u.Id = 42).Returns(Task.CompletedTask);

        // Act: Invokes the method under test with the arranged parameters.
        var userId = await handler.HandleAsync(NewCommand(), CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        userId.Should().Be(42);
    }

    [Fact]
    public async Task HandleAsync_WhenCreateAsyncThrows_MustRecordNothingAndLetTheExceptionPropagate()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // A duplicate email is the common failure; the worker turns the exception into a Failed status.
        var handler = CreateHandler();
        var command = NewCommand();
        _userService.Setup(s => s.CreateAsync(It.IsAny<User>())).ThrowsAsync(new EmailAlreadyExistsException(command.Email));

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => handler.HandleAsync(command, CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().ThrowAsync<EmailAlreadyExistsException>();
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    private static CreateUserCommand NewCommand()
        => new(Guid.NewGuid(), "Brand New", "User", "brandnewuser@example.com", new DateOnly(1995, 4, 12), IsActive: true, PasswordHash: "hashed-12345");

    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IUserLogService> _userLogService = new();
    private CreateUserCommandHandler CreateHandler() => new(_userService.Object, _userLogService.Object);
}
