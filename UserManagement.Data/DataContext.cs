using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

        var users = new[]
        {
            new User { Id = 1, Forename = "Peter", Surname = "Loew", Email = "ploew@example.com", IsActive = true, DateOfBirth = new DateOnly(1955, 3, 22) },
            new User { Id = 2, Forename = "Benjamin Franklin", Surname = "Gates", Email = "bfgates@example.com", IsActive = true, DateOfBirth = new DateOnly(1968, 7, 15) },
            new User { Id = 3, Forename = "Castor", Surname = "Troy", Email = "ctroy@example.com", IsActive = false, DateOfBirth = new DateOnly(1970, 11, 2) },
            new User { Id = 4, Forename = "Memphis", Surname = "Raines", Email = "mraines@example.com", IsActive = true, DateOfBirth = new DateOnly(1965, 5, 30) },
            new User { Id = 5, Forename = "Stanley", Surname = "Goodspeed", Email = "sgodspeed@example.com", IsActive = true, DateOfBirth = new DateOnly(1972, 9, 18) },
            new User { Id = 6, Forename = "H.I.", Surname = "McDunnough", Email = "himcdunnough@example.com", IsActive = true, DateOfBirth = new DateOnly(1958, 2, 10) },
            new User { Id = 7, Forename = "Cameron", Surname = "Poe", Email = "cpoe@example.com", IsActive = false, DateOfBirth = new DateOnly(1975, 4, 5) },
            new User { Id = 8, Forename = "Edward", Surname = "Malus", Email = "emalus@example.com", IsActive = false, DateOfBirth = new DateOnly(1969, 12, 25) },
            new User { Id = 9, Forename = "Damon", Surname = "Macready", Email = "dmacready@example.com", IsActive = false, DateOfBirth = new DateOnly(1960, 8, 14) },
            new User { Id = 10, Forename = "Johnny", Surname = "Blaze", Email = "jblaze@example.com", IsActive = true, DateOfBirth = new DateOnly(1980, 6, 21) },
            new User { Id = 11, Forename = "Robin", Surname = "Feld", Email = "rfeld@example.com", IsActive = true, DateOfBirth = new DateOnly(1963, 1, 9) },
        };

        model.Entity<User>().HasData(users);

        model.Entity<UserLog>().HasData(users.Select(u => new UserLog
        {
            Id = u.Id,
            UserId = u.Id,
            Action = UserLogAction.Created,
            Timestamp = DateTime.UtcNow,
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

    public Task DeleteAsync<TEntity>(TEntity entity) where TEntity : class
        => PersistAsync(() => base.Remove(entity));

    private async Task PersistAsync(Action trackerOperation)
    {
        trackerOperation();
        await SaveChangesAsync();
    }
}
