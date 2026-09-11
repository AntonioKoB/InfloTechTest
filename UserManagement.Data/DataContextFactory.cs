using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UserManagement.Data;

// For the dotnet ef design-time tooling only. Without it EF runs the API's Program.cs, and its startup
// Migrate(), just to build a context. The connection string is never opened at design time.
public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer("Server=localhost;Database=InfloUsersDB;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new DataContext(options);
    }
}
