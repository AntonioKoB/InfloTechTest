using System;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Services.Commands;
using UserManagement.Services.Messaging;

namespace UserManagement.Data.Tests;

public class InMemoryMessageBusTests
{
    [Fact]
    public async Task ConsumeAsync_WhenACommandWasPublished_MustYieldIt()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var bus = new InMemoryMessageBus();
        var command = new DeleteUserCommand(Guid.NewGuid(), 5);
        await bus.PublishAsync(command);

        // Act: Invokes the method under test with the arranged parameters.
        var consumed = await bus.ReadAsync(1);

        // Assert: Verifies that the action of the method under test behaves as expected.
        consumed.Should().ContainSingle().Which.Should().BeSameAs(command);
    }

    [Fact]
    public async Task ConsumeAsync_WhenTwoCommandsWerePublished_MustYieldThemInPublishOrder()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var bus = new InMemoryMessageBus();
        var first = new DeleteUserCommand(Guid.NewGuid(), 1);
        var second = new DeleteUserCommand(Guid.NewGuid(), 2);
        await bus.PublishAsync(first);
        await bus.PublishAsync(second);

        // Act: Invokes the method under test with the arranged parameters.
        var consumed = await bus.ReadAsync(2);

        // Assert: Verifies that the action of the method under test behaves as expected.
        consumed.Should().ContainInOrder(first, second);
    }

    [Fact]
    public async Task ConsumeAsync_WhenNothingIsPublishedYet_MustWaitForThePublish()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var bus = new InMemoryMessageBus();
        var command = new DeleteUserCommand(Guid.NewGuid(), 5);
        var reading = bus.ReadAsync(1);
        await Task.Delay(100);
        reading.IsCompleted.Should().BeFalse();

        // Act: Invokes the method under test with the arranged parameters.
        await bus.PublishAsync(command);

        // Assert: Verifies that the action of the method under test behaves as expected.
        (await reading).Should().ContainSingle().Which.Should().BeSameAs(command);
    }

    [Fact]
    public async Task ConsumeAsync_WhenCancelled_MustStopWithOperationCanceled()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var bus = new InMemoryMessageBus();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var act = async () =>
        {
            await foreach (var _ in bus.ConsumeAsync(cts.Token))
            {
            }
        };

        // Act & Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
