using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UserManagement.Data;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddDbContext<DataContext>(options => options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                // Retries the transient faults a hosted SQL Server (Azure SQL in particular) is documented to
                // throw: throttling, failover, dropped connections. The budget is deliberately short - roughly
                // nine seconds worst case - so it fits inside the Blazor client's own per-attempt timeout
                // instead of both layers retrying on top of each other. Nothing in the data layer opens a user
                // transaction, so no ExecuteAsync wrapping is needed.
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null)))
            .AddScoped<IDataContext>(sp => sp.GetRequiredService<DataContext>());
}
