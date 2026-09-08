using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using UserManagement.Data;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddDbContext<DataContext>(options => options
                .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                // UserLog.Timestamp seeds via DateTime.UtcNow, deliberately re-evaluated on every model
                // build rather than a fixed historical value (see OnModelCreating) - EF's migrations
                // validation otherwise treats that as an unmigrated model change and refuses to migrate.
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)))
            .AddScoped<IDataContext>(sp => sp.GetRequiredService<DataContext>());
}
