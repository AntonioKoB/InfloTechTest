using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using UserManagement.Api.Caching;
using UserManagement.Api.Commands;
using UserManagement.Models;
using UserManagement.Services.Commands;
using UserManagement.Services.Domain.Exceptions;
using UserManagement.Services.Messaging;

namespace UserManagement.Api.Tests;

/// <summary>
/// The worker is the only consumer of the bus. Each test starts the real hosted service against a real
/// in-memory bus and status store, publishes, and waits for the status to leave Pending - the same path the
/// API's caller polls.
/// </summary>
public class CommandWorkerTests : IAsyncLifetime
{
    public sealed record TestCommand(Guid CommandId) : ICommand;

    [Fact]
    public async Task ExecuteAsync_MustDispatchEachCommandToItsHandlerAndMarkItCompletedWithTheReturnedUserId()
    {
        // Arrange
        var handler = new Mock<ICommandHandler<TestCommand>>();
        handler.Setup(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(42);
        await StartWorkerAsync(s => s.AddSingleton(handler.Object));
        var command = new TestCommand(Guid.NewGuid());

        // Act
        await _bus.PublishAsync(command);
        var status = await WaitForOutcomeAsync(command.CommandId);

        // Assert
        status.Should().Be(new CommandStatus(command.CommandId, CommandState.Completed, 42, null));
        handler.Verify(h => h.HandleAsync(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAHandlerThrows_MustMarkTheCommandFailedWithTheExceptionMessage()
    {
        // Arrange
        // A duplicate email is the expected failure: the service throws, the status carries its message, and
        // that message is what the caller shows.
        var handler = new Mock<ICommandHandler<TestCommand>>();
        handler.Setup(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(new EmailAlreadyExistsException("taken@example.com"));
        await StartWorkerAsync(s => s.AddSingleton(handler.Object));
        var command = new TestCommand(Guid.NewGuid());

        // Act
        await _bus.PublishAsync(command);
        var status = await WaitForOutcomeAsync(command.CommandId);

        // Assert
        status.Should().Be(new CommandStatus(command.CommandId, CommandState.Failed, null, new EmailAlreadyExistsException("taken@example.com").Message));
    }

    [Fact]
    public async Task ExecuteAsync_WhenAHandlerThrows_MustLogTheFailureAtWarningWithTheException()
    {
        // Arrange
        var handler = new Mock<ICommandHandler<TestCommand>>();
        handler.Setup(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        await StartWorkerAsync(s => s.AddSingleton(handler.Object));
        var command = new TestCommand(Guid.NewGuid());

        // Act
        await _bus.PublishAsync(command);
        await WaitForOutcomeAsync(command.CommandId);

        // Assert
        _logger.Collector.GetSnapshot().Should().ContainSingle()
            .Which.Should().Match<FakeLogRecord>(r =>
                r.Level == LogLevel.Warning &&
                r.Id.Id == 1401 &&
                r.Exception is InvalidOperationException &&
                r.Message.Contains(command.CommandId.ToString()));
    }

    [Fact]
    public async Task ExecuteAsync_WhenAHandlerThrows_MustKeepProcessingTheNextCommand()
    {
        // Arrange
        // One bad command must never take the worker down with it - every later command would sit Pending
        // forever, which the caller sees as a timeout.
        var failing = new TestCommand(Guid.NewGuid());
        var following = new TestCommand(Guid.NewGuid());
        var handler = new Mock<ICommandHandler<TestCommand>>();
        handler.Setup(h => h.HandleAsync(failing, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        handler.Setup(h => h.HandleAsync(following, It.IsAny<CancellationToken>())).ReturnsAsync(7);
        await StartWorkerAsync(s => s.AddSingleton(handler.Object));

        // Act
        await _bus.PublishAsync(failing);
        await _bus.PublishAsync(following);
        var failed = await WaitForOutcomeAsync(failing.CommandId);
        var completed = await WaitForOutcomeAsync(following.CommandId);

        // Assert
        failed.State.Should().Be(CommandState.Failed);
        completed.Should().Be(new CommandStatus(following.CommandId, CommandState.Completed, 7, null));
    }

    [Fact]
    public async Task ExecuteAsync_WhenACreateUpdateOrDeleteCompletes_MustEvictTheUsersListCacheEachTime()
    {
        // Arrange
        // The list is output-cached by the API and the write now happens here, so this is where the tag is
        // evicted - after the row changed, never when the request was merely accepted.
        var create = new Mock<ICommandHandler<CreateUserCommand>>();
        create.Setup(h => h.HandleAsync(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var update = new Mock<ICommandHandler<UpdateUserCommand>>();
        update.Setup(h => h.HandleAsync(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(2);
        var delete = new Mock<ICommandHandler<DeleteUserCommand>>();
        delete.Setup(h => h.HandleAsync(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(3);
        await StartWorkerAsync(s => s.AddSingleton(create.Object).AddSingleton(update.Object).AddSingleton(delete.Object));
        var commands = new ICommand[]
        {
            new CreateUserCommand(Guid.NewGuid(), "Brand New", "User", "brandnewuser@example.com", new DateOnly(1995, 4, 12), true, "hash"),
            new UpdateUserCommand(Guid.NewGuid(), 2, "Updated", "User", "updated@example.com", new DateOnly(1990, 1, 1), true, null),
            new DeleteUserCommand(Guid.NewGuid(), 3)
        };

        // Act
        foreach (var command in commands)
        {
            await _bus.PublishAsync(command);
            await WaitForOutcomeAsync(command.CommandId);
        }

        // Assert
        _outputCache.Verify(c => c.EvictByTagAsync(OutputCachingExtensions.UsersTag, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteAsync_WhenACommandFails_MustNotEvictTheUsersListCache()
    {
        // Arrange
        var create = new Mock<ICommandHandler<CreateUserCommand>>();
        create.Setup(h => h.HandleAsync(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(new EmailAlreadyExistsException("taken@example.com"));
        await StartWorkerAsync(s => s.AddSingleton(create.Object));
        var command = new CreateUserCommand(Guid.NewGuid(), "Brand New", "User", "taken@example.com", new DateOnly(1995, 4, 12), true, "hash");

        // Act
        await _bus.PublishAsync(command);
        await WaitForOutcomeAsync(command.CommandId);

        // Assert
        _outputCache.Verify(c => c.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenALogCommandCompletes_MustNotEvictTheUsersListCache()
    {
        // Arrange
        // A log entry changes nothing the users list shows.
        var record = new Mock<ICommandHandler<RecordUserLogCommand>>();
        record.Setup(h => h.HandleAsync(It.IsAny<RecordUserLogCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(5);
        await StartWorkerAsync(s => s.AddSingleton(record.Object));
        var command = new RecordUserLogCommand(Guid.NewGuid(), new UserLog { UserId = 5, Action = UserLogAction.Viewed, Timestamp = DateTime.UtcNow });

        // Act
        await _bus.PublishAsync(command);
        await WaitForOutcomeAsync(command.CommandId);

        // Assert
        _outputCache.Verify(c => c.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_MustResolveTheHandlerFromANewScopeForEachCommand()
    {
        // Arrange
        // Handlers are scoped (they carry a DbContext); a long-lived worker must open a scope per command,
        // not hold one for its lifetime.
        var instances = new List<object>();
        await StartWorkerAsync(s => s.AddSingleton(instances).AddScoped<ICommandHandler<TestCommand>, ScopeRecordingHandler>());
        var first = new TestCommand(Guid.NewGuid());
        var second = new TestCommand(Guid.NewGuid());

        // Act
        await _bus.PublishAsync(first);
        await _bus.PublishAsync(second);
        await WaitForOutcomeAsync(first.CommandId);
        await WaitForOutcomeAsync(second.CommandId);

        // Assert
        instances.Should().HaveCount(2);
        instances.Distinct().Should().HaveCount(2);
    }

    private sealed class ScopeRecordingHandler : ICommandHandler<TestCommand>
    {
        private readonly List<object> _instances;
        public ScopeRecordingHandler(List<object> instances) => _instances = instances;

        public Task<long> HandleAsync(TestCommand command, CancellationToken cancellationToken)
        {
            _instances.Add(this);
            return Task.FromResult(0L);
        }
    }

    private async Task StartWorkerAsync(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        _provider = services.BuildServiceProvider();
        _worker = new CommandWorker(_bus, _provider.GetRequiredService<IServiceScopeFactory>(), _store, _outputCache.Object, _logger);
        await _worker.StartAsync(CancellationToken.None);
    }

    private async Task<CommandStatus> WaitForOutcomeAsync(Guid commandId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var status = await _store.GetAsync(commandId);
            if (status is not null && status.State != CommandState.Pending)
                return status;
            await Task.Delay(10);
        }

        throw new TimeoutException($"Command {commandId} was not processed within 5 seconds.");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_worker is not null)
        {
            await _worker.StopAsync(CancellationToken.None);
            _worker.Dispose();
        }

        _provider?.Dispose();
    }

    private readonly InMemoryMessageBus _bus = new();
    private readonly InMemoryCommandStatusStore _store = new();
    private readonly Mock<IOutputCacheStore> _outputCache = new();
    private readonly FakeLogger<CommandWorker> _logger = new();
    private ServiceProvider? _provider;
    private CommandWorker? _worker;
}
