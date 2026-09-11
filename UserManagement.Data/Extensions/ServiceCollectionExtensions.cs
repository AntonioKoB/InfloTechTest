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
                // Retries the transient faults Azure SQL is documented to raise. Roughly nine seconds worst
                // case, so it fits inside the Blazor client's own per-attempt timeout.
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null)))
            .AddScoped<IDataContext>(sp => sp.GetRequiredService<DataContext>());
}
