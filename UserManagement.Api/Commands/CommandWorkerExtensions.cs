using Microsoft.Extensions.DependencyInjection;

namespace UserManagement.Api.Commands;

public static class CommandWorkerExtensions
{
    public static IServiceCollection AddCommandWorker(this IServiceCollection services) => services;
}
