using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UserManagement.Api.Caching;
using UserManagement.Services.Commands;
using UserManagement.Services.Messaging;

namespace UserManagement.Api.Commands;

/// <summary>
/// The only consumer of the bus. Takes commands in order, runs each one in its own DI scope through the
/// handler registered for its type, and records the outcome for the status endpoint. A failing handler
/// marks its command Failed and is logged; it never stops the worker.
/// </summary>
public partial class CommandWorker : BackgroundService
{
    private delegate Task<long> Dispatcher(IServiceProvider services, ICommand command, CancellationToken cancellationToken);

    // One typed dispatcher per command type, built on first use. After that a command is a direct delegate
    // call into its handler, and a handler's exception arrives as itself rather than wrapped by reflection.
    private static readonly ConcurrentDictionary<Type, Dispatcher> Dispatchers = new();
    private static readonly MethodInfo DispatchMethod = typeof(CommandWorker).GetMethod(nameof(Dispatch), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly IMessageBus _bus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICommandStatusStore _statusStore;
    private readonly IOutputCacheStore _outputCache;
    private readonly ILogger<CommandWorker> _logger;

    public CommandWorker(IMessageBus bus, IServiceScopeFactory scopeFactory, ICommandStatusStore statusStore, IOutputCacheStore outputCache, ILogger<CommandWorker> logger)
    {
        _bus = bus;
        _scopeFactory = scopeFactory;
        _statusStore = statusStore;
        _outputCache = outputCache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var command in _bus.ConsumeAsync(stoppingToken))
            {
                await ProcessAsync(command, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is stopping; that is the only expected way out of the loop.
            LogWorkerStopped();
        }
        catch (Exception ex)
        {
            // Only the bus itself can throw here (each command's own failure is handled in ProcessAsync). A
            // worker that went quiet would leave every later command Pending while the API kept accepting
            // them, so this says why it died and lets the host's BackgroundService policy stop the process,
            // which the platform then restarts.
            LogWorkerFaulted(ex);
            throw;
        }
    }

    private async Task ProcessAsync(ICommand command, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userId = await DispatcherFor(command.GetType())(scope.ServiceProvider, command, cancellationToken);
            await _statusStore.MarkCompletedAsync(command.CommandId, userId);

            if (ChangesTheUsersList(command))
                await EvictUsersListAsync();
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            await _statusStore.MarkFailedAsync(command.CommandId, ex.Message);
            LogCommandFailed(ex, command.GetType().Name, command.CommandId, ex.Message);
        }
    }

    private static Dispatcher DispatcherFor(Type commandType)
        => Dispatchers.GetOrAdd(commandType, type => DispatchMethod.MakeGenericMethod(type).CreateDelegate<Dispatcher>());

    private static Task<long> Dispatch<TCommand>(IServiceProvider services, ICommand command, CancellationToken cancellationToken) where TCommand : ICommand
        => services.GetRequiredService<ICommandHandler<TCommand>>().HandleAsync((TCommand)command, cancellationToken);

    // The users list is output-cached by the API and the row changed here, so this is where the tag goes.
    // A log entry changes nothing the list shows. The eviction ignores the stopping token on purpose: a
    // write that completed must evict even if the host is shutting down.
    private static bool ChangesTheUsersList(ICommand command)
        => command is CreateUserCommand or UpdateUserCommand or DeleteUserCommand;

    private Task EvictUsersListAsync()
        => _outputCache.EvictByTagAsync(OutputCachingExtensions.UsersTag, CancellationToken.None).AsTask();

    [LoggerMessage(EventId = 1401, Level = LogLevel.Warning, Message = "Command {CommandType} {CommandId} failed: {Error}")]
    private partial void LogCommandFailed(Exception exception, string commandType, Guid commandId, string error);

    [LoggerMessage(EventId = 1402, Level = LogLevel.Information, Message = "Command worker stopped with the host")]
    private partial void LogWorkerStopped();

    [LoggerMessage(EventId = 1403, Level = LogLevel.Error, Message = "Command worker faulted; accepted commands will not run until the host restarts")]
    private partial void LogWorkerFaulted(Exception exception);
}
