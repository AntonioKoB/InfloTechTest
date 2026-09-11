using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UserManagement.Services.Commands;
using UserManagement.Services.Messaging;

namespace UserManagement.Api.Commands;

public class CommandWorker : BackgroundService
{
    public CommandWorker(IMessageBus bus, IServiceScopeFactory scopeFactory, ICommandStatusStore statusStore, IOutputCacheStore outputCache, ILogger<CommandWorker> logger)
    {
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => throw new NotImplementedException();
}
