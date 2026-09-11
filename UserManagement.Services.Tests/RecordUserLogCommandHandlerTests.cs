using System;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Commands;

namespace UserManagement.Data.Tests;

public class RecordUserLogCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_MustPersistTheEntryExactlyAsCarried()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        var entry = new UserLog { UserId = 5, Action = UserLogAction.Updated, Timestamp = DateTime.UtcNow, BeforeJson = "{}", AfterJson = "{}" };

        // Act: Invokes the method under test with the arranged parameters.
        await handler.HandleAsync(new RecordUserLogCommand(Guid.NewGuid(), entry), CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _dataContext.Verify(d => d.CreateAsync(entry), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_MustReturnTheEntrysUserId()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var handler = CreateHandler();
        var entry = new UserLog { UserId = 5, Action = UserLogAction.Viewed, Timestamp = DateTime.UtcNow };

        // Act: Invokes the method under test with the arranged parameters.
        var userId = await handler.HandleAsync(new RecordUserLogCommand(Guid.NewGuid(), entry), CancellationToken.None);

        // Assert: Verifies that the action of the method under test behaves as expected.
        userId.Should().Be(5);
    }

    private readonly Mock<IDataContext> _dataContext = new();
    private RecordUserLogCommandHandler CreateHandler() => new(_dataContext.Object);
}
