using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Api.Commands;

public static class CommandWorkerExtensions
{
    /// <summary>
    /// Hosts the command worker in the API process.
    /// </summary>
    public static IServiceCollection AddCommandWorker(this IServiceCollection services)
        => services.AddHostedService<CommandWorker>();
}
