using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data.Exceptions;
using UserManagement.Models;

namespace UserManagement.Data;

public class DataContext : DbContext, IDataContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>().HasIndex(u => u.Email).IsUnique();

        // PasswordHasher output is salted, so it differs on every call - the seed value has to be a fixed
        // literal for the same reason as the fixed seed timestamp below (HasData values are part of the
        // model). This is the PasswordHasher (v3, PBKDF2) hash of "12345", the documented seed password
        // that every seeded user shares so a reviewer can sign in as any of them.
        const string SeedPasswordHash = "AQAAAAIAAYagAAAAEPZnlhpJfpSdxqhrGlbHhIasKXCG6qHDec7pxnfgcQXKywwpDcmZ8UEqvYWj8sN3+g==";

        var users = new[]
        {
            new User { Id = 1, Forename = "Peter", Surname = "Loew", Email = "ploew@example.com", IsActive = true, DateOfBirth = new DateOnly(1955, 3, 22), PasswordHash = SeedPasswordHash },
            new User { Id = 2, Forename = "Benjamin Franklin", Surname = "Gates", Email = "bfgates@example.com", IsActive = true, DateOfBirth = new DateOnly(1968, 7, 15), PasswordHash = SeedPasswordHash },
            new User { Id = 3, Forename = "Castor", Surname = "Troy", Email = "ctroy@example.com", IsActive = false, DateOfBirth = new DateOnly(1970, 11, 2), PasswordHash = SeedPasswordHash },
            new User { Id = 4, Forename = "Memphis", Surname = "Raines", Email = "mraines@example.com", IsActive = true, DateOfBirth = new DateOnly(1965, 5, 30), PasswordHash = SeedPasswordHash },
            new User { Id = 5, Forename = "Stanley", Surname = "Goodspeed", Email = "sgodspeed@example.com", IsActive = true, DateOfBirth = new DateOnly(1972, 9, 18), PasswordHash = SeedPasswordHash },
            new User { Id = 6, Forename = "H.I.", Surname = "McDunnough", Email = "himcdunnough@example.com", IsActive = true, DateOfBirth = new DateOnly(1958, 2, 10), PasswordHash = SeedPasswordHash },
            new User { Id = 7, Forename = "Cameron", Surname = "Poe", Email = "cpoe@example.com", IsActive = false, DateOfBirth = new DateOnly(1975, 4, 5), PasswordHash = SeedPasswordHash },
            new User { Id = 8, Forename = "Edward", Surname = "Malus", Email = "emalus@example.com", IsActive = false, DateOfBirth = new DateOnly(1969, 12, 25), PasswordHash = SeedPasswordHash },
            new User { Id = 9, Forename = "Damon", Surname = "Macready", Email = "dmacready@example.com", IsActive = false, DateOfBirth = new DateOnly(1960, 8, 14), PasswordHash = SeedPasswordHash },
            new User { Id = 10, Forename = "Johnny", Surname = "Blaze", Email = "jblaze@example.com", IsActive = true, DateOfBirth = new DateOnly(1980, 6, 21), PasswordHash = SeedPasswordHash },
            new User { Id = 11, Forename = "Robin", Surname = "Feld", Email = "rfeld@example.com", IsActive = true, DateOfBirth = new DateOnly(1963, 1, 9), PasswordHash = SeedPasswordHash },
        };

        model.Entity<User>().HasData(users);

        // Fixed, deterministic value - not DateTime.UtcNow. HasData seed values are part of the model itself,
        // so a value that changes on every model build makes EF's migrations validation (correctly) treat
        // every startup as "the model has pending changes", which forces disabling that check entirely -
        // losing the safety net that catches a genuinely forgotten migration. A static value keeps that
        // check meaningful.
        var seedTimestamp = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);

        model.Entity<UserLog>().HasData(users.Select(u => new UserLog
        {
            Id = u.Id,
            UserId = u.Id,
            Action = UserLogAction.Created,
            Timestamp = seedTimestamp,
            AfterJson = JsonSerializer.Serialize(u)
        }));
    }

    public DbSet<User>? Users { get; set; }
    public DbSet<UserLog>? UserLogs { get; set; }

    public async Task<IEnumerable<TEntity>> GetAllAsync<TEntity>() where TEntity : class
        => await base.Set<TEntity>().ToListAsync();

    public async Task<TEntity?> GetByIdAsync<TEntity>(object id) where TEntity : class
        => await base.Set<TEntity>().FindAsync(id);

    public async Task<TEntity?> FirstOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        => await base.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(predicate);

    public async Task<IEnumerable<TEntity>> WhereAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        => await base.Set<TEntity>().AsNoTracking().Where(predicate).ToListAsync();

    public async Task<IReadOnlyList<TEntity>> GetPageAsync<TEntity, TKey>(Expression<Func<TEntity, TKey>> orderBy, bool descending, int skip, int take) where TEntity : class
    {
        var query = base.Set<TEntity>().AsNoTracking();
        query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        return await query.Skip(skip).Take(take).ToListAsync();
    }

    public Task<int> CountAsync<TEntity>() where TEntity : class
        => base.Set<TEntity>().CountAsync();

    public Task CreateAsync<TEntity>(TEntity entity) where TEntity : class
        => PersistAsync(() => base.Add(entity));

    public Task UpdateAsync<TEntity>(TEntity entity) where TEntity : class
        => PersistAsync(() => base.Update(entity));

    public Task DeleteWhereAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        => base.Set<TEntity>().Where(predicate).ExecuteDeleteAsync();

    private async Task PersistAsync(Action trackerOperation)
    {
        trackerOperation();

        try
        {
            await SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // EF Core throws this when the affected-row-count doesn't match what it expected - most
            // commonly, the row was deleted by someone else between being fetched and being saved. Wrapped
            // so callers don't need to reference EF Core directly to handle it.
            throw new ConcurrencyConflictException("The entity being saved no longer exists - it may have been deleted by another request.", ex);
        }
    }
}
