using System;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Commands;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Data.Tests;

public class DeleteUserCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_MustDeleteTheUserById()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        SetupExistingUser();

        // Act: Invokes the method under test with the arranged parameters.
        await handler.HandleAsync(new DeleteUserCommand(Guid.NewGuid(), 5), CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _userService.Verify(s => s.DeleteAsync(5), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenTheUserExists_MustRecordADeletedLogWithItAsTheBeforeSnapshot()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        var existing = SetupExistingUser();

        // Act: Invokes the method under test with the arranged parameters.
        await handler.HandleAsync(new DeleteUserCommand(Guid.NewGuid(), 5), CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _userLogService.Verify(s => s.RecordAsync(5, UserLogAction.Deleted, existing, null), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenTheUserDoesNotExist_MustStillDeleteAndRecordNothing()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        _userService.Setup(s => s.GetByIdAsync(999, false)).ReturnsAsync((User?)null);

        // Act: Invokes the method under test with the arranged parameters.
        await handler.HandleAsync(new DeleteUserCommand(Guid.NewGuid(), 999), CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _userService.Verify(s => s.DeleteAsync(999), Times.Once);
        _userLogService.Verify(s => s.RecordAsync(It.IsAny<long>(), It.IsAny<UserLogAction>(), It.IsAny<User?>(), It.IsAny<User?>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_MustReturnTheUserId()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        SetupExistingUser();

        // Act: Invokes the method under test with the arranged parameters.
        var userId = await handler.HandleAsync(new DeleteUserCommand(Guid.NewGuid(), 5), CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        userId.Should().Be(5);
    }

    private User SetupExistingUser()
    {
        var existing = new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) };
        _userService.Setup(s => s.GetByIdAsync(5, false)).ReturnsAsync(existing);
        return existing;
    }

    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IUserLogService> _userLogService = new();
    private DeleteUserCommandHandler CreateHandler() => new(_userService.Object, _userLogService.Object);
}
