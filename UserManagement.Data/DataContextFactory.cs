using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UserManagement.Data;

// Used only by the `dotnet ef` design-time tooling (e.g. `migrations add`), which needs to construct a
// DataContext without a running DI container. Without this, EF falls back to invoking the Web project's
// Program.cs via reflection to build one, which would also run its startup Database.Migrate() call as a
// side effect of scaffolding a migration. The connection string here is never actually opened at design
// time - migrations add only needs the model, not a live connection.
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
