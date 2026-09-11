using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
            // The host is stopping; that is the only way out of the loop.
        }
    }

    private async Task ProcessAsync(ICommand command, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userId = await DispatchAsync(command, scope.ServiceProvider, cancellationToken);
            await _statusStore.MarkCompletedAsync(command.CommandId, userId);

            // The users list is output-cached by the API; the row changed here, so this is where it is evicted.
            if (command is CreateUserCommand or UpdateUserCommand or DeleteUserCommand)
                await _outputCache.EvictByTagAsync(OutputCachingExtensions.UsersTag, CancellationToken.None);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            await _statusStore.MarkFailedAsync(command.CommandId, ex.Message);
            LogCommandFailed(ex, command.GetType().Name, command.CommandId, ex.Message);
        }
    }

    private static async Task<long> DispatchAsync(ICommand command, IServiceProvider services, CancellationToken cancellationToken)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        var handler = services.GetRequiredService(handlerType);
        var handle = handlerType.GetMethod(nameof(ICommandHandler<ICommand>.HandleAsync))!;

        try
        {
            return await (Task<long>)handle.Invoke(handler, [command, cancellationToken])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // A handler that throws before its first await surfaces here wrapped; the status wants the cause.
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    [LoggerMessage(EventId = 1401, Level = LogLevel.Warning, Message = "Command {CommandType} {CommandId} failed: {Error}")]
    private partial void LogCommandFailed(Exception exception, string commandType, Guid commandId, string error);
}
