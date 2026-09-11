using System;
using System.Threading.Tasks;
using UserManagement.Services.Commands;

namespace UserManagement.Data.Tests;

public class InMemoryCommandStatusStoreTests
{
    [Fact]
    public async Task GetAsync_WhenTheIdIsUnknown_MustReturnNull()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var store = new InMemoryCommandStatusStore();

        // Act: Invokes the method under test with the arranged parameters.
        var status = await store.GetAsync(Guid.NewGuid());

        // Assert: Verifies that the action of the method under test behaves as expected.
        status.Should().BeNull();
    }

    [Fact]
    public async Task MarkPendingAsync_ThenGetAsync_MustReturnPendingWithNoUserIdOrError()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var store = new InMemoryCommandStatusStore();
        var id = Guid.NewGuid();

        // Act: Invokes the method under test with the arranged parameters.
        await store.MarkPendingAsync(id);

        // Assert: Verifies that the action of the method under test behaves as expected.
        var status = await store.GetAsync(id);
        status.Should().Be(new CommandStatus(id, CommandState.Pending, null, null));
    }

    [Fact]
    public async Task MarkCompletedAsync_MustReplacePendingWithCompletedAndTheAffectedUserId()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var store = new InMemoryCommandStatusStore();
        var id = Guid.NewGuid();
        await store.MarkPendingAsync(id);

        // Act: Invokes the method under test with the arranged parameters.
        await store.MarkCompletedAsync(id, userId: 42);

        // Assert: Verifies that the action of the method under test behaves as expected.
        var status = await store.GetAsync(id);
        status.Should().Be(new CommandStatus(id, CommandState.Completed, 42, null));
    }

    [Fact]
    public async Task MarkFailedAsync_MustReplacePendingWithFailedAndTheError()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var store = new InMemoryCommandStatusStore();
        var id = Guid.NewGuid();
        await store.MarkPendingAsync(id);

        // Act: Invokes the method under test with the arranged parameters.
        await store.MarkFailedAsync(id, "A user with email 'taken@example.com' already exists.");

        // Assert: Verifies that the action of the method under test behaves as expected.
        var status = await store.GetAsync(id);
        status.Should().Be(new CommandStatus(id, CommandState.Failed, null, "A user with email 'taken@example.com' already exists."));
    }
}
