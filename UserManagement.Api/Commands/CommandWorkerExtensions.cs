using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Api.Commands;

public static class CommandWorkerExtensions
{
    /// <summary>
    /// Hosts the command worker in the API process. Without it commands are accepted and never executed.
    /// </summary>
    public static IServiceCollection AddCommandWorker(this IServiceCollection services)
        => services.AddHostedService<CommandWorker>();
}
