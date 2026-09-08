using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UserManagement.Data;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
        => services
            .AddDbContext<DataContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")))
            .AddScoped<IDataContext>(sp => sp.GetRequiredService<DataContext>());
}
