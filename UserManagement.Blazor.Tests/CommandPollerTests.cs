using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Refit;
using UserManagement.Api.Contracts.Commands;
using UserManagement.Blazor.Api;

namespace UserManagement.Blazor.Tests;

/// <summary>
/// The poller is the UI's only way to learn what happened to an accepted command. It runs here with a
/// millisecond interval and timeout so the timing behaviour is exercised without a fake clock.
/// </summary>
public class CommandPollerTests
{
    [Fact]
    public async Task WaitForOutcomeAsync_WhenTheFirstReadIsCompleted_MustReturnIt()
    {
        // Arrange
        var id = Guid.NewGuid();
        var completed = Status(id, CommandState.Completed, userId: 17);
        _usersApi.Setup(a => a.GetCommandStatusAsync(id)).ReturnsAsync(completed);

        // Act
        var outcome = await CreatePoller().WaitForOutcomeAsync(id);

        // Assert
        outcome.Should().BeSameAs(completed);
        _usersApi.Verify(a => a.GetCommandStatusAsync(id), Times.Once);
    }

    [Fact]
    public async Task WaitForOutcomeAsync_WhenTheFirstReadIsFailed_MustReturnIt()
    {
        // Arrange
        var id = Guid.NewGuid();
        var failed = Status(id, CommandState.Failed, error: "A user with email 'taken@example.com' already exists.");
        _usersApi.Setup(a => a.GetCommandStatusAsync(id)).ReturnsAsync(failed);

        // Act
        var outcome = await CreatePoller().WaitForOutcomeAsync(id);

        // Assert
        outcome.Should().BeSameAs(failed);
    }

    [Fact]
    public async Task WaitForOutcomeAsync_WhilePending_MustKeepReadingAndReturnTheFirstOutcome()
    {
        // Arrange
        var id = Guid.NewGuid();
        var completed = Status(id, CommandState.Completed, userId: 17);
        _usersApi.SetupSequence(a => a.GetCommandStatusAsync(id))
            .ReturnsAsync(Status(id, CommandState.Pending))
            .ReturnsAsync(Status(id, CommandState.Pending))
            .ReturnsAsync(completed);

        // Act
        var outcome = await CreatePoller().WaitForOutcomeAsync(id);

        // Assert
        outcome.Should().BeSameAs(completed);
        _usersApi.Verify(a => a.GetCommandStatusAsync(id), Times.Exactly(3));
    }

    [Fact]
    public async Task WaitForOutcomeAsync_WhenStillPendingPastTheTimeout_MustThrowTimeoutException()
    {
        // Arrange
        // A command the API lost (a restart between accept and execute) stays Pending forever; the caller
        // must get an error, not a spinner.
        var id = Guid.NewGuid();
        _usersApi.Setup(a => a.GetCommandStatusAsync(id)).ReturnsAsync(Status(id, CommandState.Pending));
        var poller = CreatePoller(timeout: TimeSpan.FromMilliseconds(50));

        // Act
        var act = () => poller.WaitForOutcomeAsync(id);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
        _usersApi.Verify(a => a.GetCommandStatusAsync(id), Times.AtLeastOnce);
    }

    [Fact]
    public async Task WaitForOutcomeAsync_WhenAlreadyCancelled_MustThrowWithoutReading()
    {
        // Arrange
        var id = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => CreatePoller().WaitForOutcomeAsync(id, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        _usersApi.Verify(a => a.GetCommandStatusAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task WaitForOutcomeAsync_WhenTheStatusCallFails_MustLetTheApiExceptionPropagate()
    {
        // Arrange
        // The poller reports what the API said; it never invents a status for an id the API does not know.
        var id = Guid.NewGuid();
        var notFound = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, $"https://localhost/api/commands/{id}"),
            HttpMethod.Get,
            new HttpResponseMessage(HttpStatusCode.NotFound),
            new RefitSettings());
        _usersApi.Setup(a => a.GetCommandStatusAsync(id)).ThrowsAsync(notFound);

        // Act
        var act = () => CreatePoller().WaitForOutcomeAsync(id);

        // Assert
        (await act.Should().ThrowAsync<ApiException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static CommandStatusResponse Status(Guid id, CommandState state, long? userId = null, string? error = null)
        => new() { CommandId = id, State = state, UserId = userId, Error = error };

    private CommandPoller CreatePoller(TimeSpan? interval = null, TimeSpan? timeout = null)
        => new(_usersApi.Object, interval ?? TimeSpan.FromMilliseconds(1), timeout ?? TimeSpan.FromSeconds(5));

    private readonly Mock<IUsersApi> _usersApi = new();
}
