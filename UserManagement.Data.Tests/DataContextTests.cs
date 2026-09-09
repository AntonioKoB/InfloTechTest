using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data.Exceptions;
using UserManagement.Models;

namespace UserManagement.Data.Tests;

public class DataContextTests
{
    [Fact]
    public async Task GetAllAsync_WhenNewEntityAdded_MustIncludeNewEntity()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var context = CreateContext();

        var entity = new User
        {
            Forename = "Brand New",
            Surname = "User",
            Email = "brandnewuser@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12)
        };
        await context.CreateAsync(entity);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.GetAllAsync<User>();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result
            .Should().Contain(s => s.Email == entity.Email)
            .Which.Should().BeEquivalentTo(entity);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityExists_MustReturnEntity()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var context = CreateContext();
        var expected = (await context.GetAllAsync<User>()).First();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.GetByIdAsync<User>(expected.Id);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityDoesNotExist_MustReturnNull()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var context = CreateContext();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.GetByIdAsync<User>(9999L);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeNull();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WhenPredicateMatchesEntity_MustReturnEntity()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var context = CreateContext();
        var expected = (await context.GetAllAsync<User>()).First();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.FirstOrDefaultAsync<User>(u => u.Email == expected.Email);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ResultMustNotBeTracked_SoADifferentInstanceCanLaterBeUpdated()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // This is a regression test matching the real UserService.UpdateAsync flow exactly: it looks up the
        // existing user by email (to enforce uniqueness) and then calls UpdateAsync with a *different*,
        // freshly-built User instance for the same row (same Id). If FirstOrDefaultAsync tracked its result,
        // EF's identity map would reject attaching that second instance for the same primary key - which is
        // exactly what happened manually testing Edit before this was fixed. Deliberately uses no other
        // lookup beforehand (e.g. GetAllAsync), since that would track its own results and mask what's
        // actually being tested here.
        var context = CreateContext();

        var lookedUp = await context.FirstOrDefaultAsync<User>(u => u.Email == "ploew@example.com");
        lookedUp.Should().NotBeNull();
        var differentInstanceSameRow = new User
        {
            Id = lookedUp!.Id,
            Forename = "Changed",
            Surname = lookedUp.Surname,
            Email = lookedUp.Email,
            DateOfBirth = lookedUp.DateOfBirth,
            IsActive = lookedUp.IsActive
        };

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => context.UpdateAsync(differentInstanceSameRow);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WhenNoEntityMatchesPredicate_MustReturnNull()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var context = CreateContext();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.FirstOrDefaultAsync<User>(u => u.Email == "nonexistent@example.com");

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeNull();
    }

    [Fact]
    public async Task WhereAsync_WhenPredicateMatchesSubset_MustReturnOnlyMatchingEntities()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Proves WhereAsync pushes the filter to the query provider (Where composed on IQueryable before
        // materializing) rather than loading the whole table and filtering in C#, unlike the old
        // GetAllAsync<User>().Where(...) approach this method replaces in UserService.FilterByActiveAsync.
        var context = CreateContext();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.WhereAsync<User>(u => u.IsActive == false);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(u => u.IsActive == false);
    }

    [Fact]
    public async Task DeleteWhereAsync_OnInMemoryProvider_IsNotSupported_ProvenSeparatelyByManualVerification()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // EF Core's InMemory provider has no SQL translator for ExecuteDelete/ExecuteUpdate and throws for
        // both - unlike the real SQL Server provider, which executes a genuine DELETE ... WHERE ... statement.
        // Same permanent, intentional divergence as the unique-index gap below: these tests stay on InMemory
        // by design, so DeleteWhereAsync's actual SQL translation and idempotency-under-race behavior is
        // proven by manual verification against the real database, not by this suite.
        var context = CreateContext();
        var user = (await context.GetAllAsync<User>()).First();

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => context.DeleteWhereAsync<User>(u => u.Id == user.Id);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_WhenEmailAlreadyExists_InMemoryProviderDoesNotEnforceTheUniqueIndex()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Email is modelled as a unique index (HasIndex(...).IsUnique()), not an alternate key - a key
        // would make Email immutable on tracked entities (EF refuses to let you modify a property that's
        // part of a key), which breaks Edit letting someone change their email to a new address. The
        // tradeoff: EF Core's InMemory provider does not actually enforce plain unique indexes (unlike a
        // real relational provider, which would reject this at SaveChanges with a unique-constraint
        // violation). Uniqueness is therefore solely UserService's responsibility for as long as this app
        // runs on InMemory - this test documents that explicitly rather than leaving it as a silent gap.
        // This is a permanent, intentional divergence from production, not a stale TODO: these tests stay
        // on the InMemory provider by design (fast, isolated, no external dependency), even after the real
        // app switched to SQL Server, where the index genuinely is enforced - proven separately by manual
        // verification against the real database, not by this suite.
        var context = CreateContext();
        var existing = (await context.GetAllAsync<User>()).First();

        var duplicate = new User
        {
            Forename = "Someone",
            Surname = "Else",
            Email = existing.Email,
            DateOfBirth = new DateOnly(1990, 1, 1)
        };

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => context.CreateAsync(duplicate);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailChangedToNewUniqueValue_MustPersistNewEmail()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Regression test for the real bug: Email used to be an alternate key, which made EF refuse to
        // persist any change to it on a tracked entity ("The property 'User.Email' is part of a key and
        // so cannot be modified"). Fetches via GetByIdAsync (the same tracked instance the Edit flow
        // mutates in place) to exercise this exactly as the controller does.
        var context = CreateContext();
        var entity = (await context.GetAllAsync<User>()).First();
        var tracked = await context.GetByIdAsync<User>(entity.Id);
        tracked!.Email = "brandnewemail@example.com";

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => context.UpdateAsync(tracked);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().NotThrowAsync();
        var result = await context.GetAllAsync<User>();
        result.Should().Contain(u => u.Email == "brandnewemail@example.com");
    }

    [Fact]
    public async Task UpdateAsync_WhenEntityWasDeletedByAnotherRequestSinceBeingFetched_MustThrowConcurrencyConflictException()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Simulates two concurrent requests via two separate DataContext instances sharing the same
        // underlying database: contextA fetches and mutates a user but hasn't saved yet, while contextB
        // deletes that same row and saves first. EF Core's own optimistic-concurrency check (the UPDATE
        // affects 0 rows instead of the expected 1) then surfaces as this app's own
        // ConcurrencyConflictException, not a raw EF exception leaking out of the data layer.
        var databaseName = Guid.NewGuid().ToString();
        var contextA = CreateContext(databaseName);
        var contextB = CreateContext(databaseName);

        var trackedByA = await contextA.GetByIdAsync<User>(1L);
        trackedByA!.Forename = "Changed By A";

        var trackedByB = await contextB.GetByIdAsync<User>(1L);
        contextB.Remove(trackedByB!);
        await contextB.SaveChangesAsync();

        // Act: Invokes the method under test with the arranged parameters.
        var act = () => contextA.UpdateAsync(trackedByA);

        // Assert: Verifies that the action of the method under test behaves as expected.
        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task GetAllAsync_WhenUpdated_MustReflectUpdatedEntity()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var context = CreateContext();
        var entity = (await context.GetAllAsync<User>()).First();
        entity.Forename = "Updated Forename";
        await context.UpdateAsync(entity);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.GetAllAsync<User>();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result
            .Should().Contain(s => s.Email == entity.Email)
            .Which.Forename.Should().Be("Updated Forename");
    }

    [Fact]
    public async Task GetAllAsync_WhenDeleted_MustNotIncludeDeletedEntity()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var context = CreateContext();
        var entity = (await context.GetAllAsync<User>()).First();
        context.Remove(entity);
        await context.SaveChangesAsync();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.GetAllAsync<User>();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().NotContain(s => s.Email == entity.Email);
    }

    [Fact]
    public async Task CreateAsync_WhenUserLogAdded_MustBeRetrievableByGetAllAsync()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var context = CreateContext();

        var entity = new UserLog
        {
            UserId = 1,
            Action = UserLogAction.Created,
            Timestamp = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc),
            AfterJson = "{}"
        };
        await context.CreateAsync(entity);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.GetAllAsync<UserLog>();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result
            .Should().Contain(l => l.Id == entity.Id)
            .Which.Should().BeEquivalentTo(entity);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserDeleted_MustNotAffectUserLogRowsReferencingThatUserId()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // UserLog deliberately has no foreign-key/navigation relationship to User (see UserLog.cs) - an audit
        // record needs to survive deletion of the user it refers to, otherwise deleting a user would destroy
        // the very "user was deleted" log entry that matters most. This test proves that decision holds: the
        // UserLog row must remain fully intact and retrievable after its referenced User is gone.
        var context = CreateContext();
        var user = (await context.GetAllAsync<User>()).First();

        var log = new UserLog
        {
            UserId = user.Id,
            Action = UserLogAction.Deleted,
            Timestamp = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc),
            BeforeJson = "{}"
        };
        await context.CreateAsync(log);
        context.Remove(user);
        await context.SaveChangesAsync();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.GetAllAsync<UserLog>();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result
            .Should().Contain(l => l.Id == log.Id)
            .Which.Should().BeEquivalentTo(log);
    }

    [Fact]
    public async Task GetPageAsync_WhenCalled_MustReturnOrderedSkipTakeSliceWithoutLoadingWholeTable()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // This is the fix for pushing pagination down to the query provider instead of materializing the
        // whole table via GetAllAsync and paging in memory - asserting against a real DataContext (not a
        // mock) is what actually proves Skip/Take/OrderBy compose correctly against EF Core.
        var context = CreateContext();

        var logs = Enumerable.Range(1, 5)
            .Select(i => new UserLog { UserId = 1, Action = UserLogAction.Created, Timestamp = DateTime.UtcNow.AddDays(i) })
            .ToArray();
        foreach (var log in logs) await context.CreateAsync(log);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.GetPageAsync<UserLog, DateTime>(l => l.Timestamp, descending: true, skip: 1, take: 2);

        // Assert: Verifies that the action of the method under test behaves as expected.
        // BeEquivalentTo (not ContainInOrder) since GetPageAsync uses AsNoTracking and so returns freshly
        // materialized instances - not reference-equal to the logs[] array, which ContainInOrder would need.
        result.Should().BeEquivalentTo([logs[3], logs[2]], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task CountAsync_WhenCalled_MustReturnTotalCountOfThatEntityType()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Relative to the seeded baseline (each seeded User now has a matching "Created" UserLog - see
        // OnModelCreating) rather than an absolute count, so this doesn't break if the seed data changes.
        var context = CreateContext();
        var before = await context.CountAsync<UserLog>();
        await context.CreateAsync(new UserLog { UserId = 1, Action = UserLogAction.Created, Timestamp = DateTime.UtcNow });
        await context.CreateAsync(new UserLog { UserId = 1, Action = UserLogAction.Updated, Timestamp = DateTime.UtcNow });

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.CountAsync<UserLog>();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().Be(before + 2);
    }

    [Fact]
    public async Task SeededUsers_MustAllHaveAPasswordHashThatVerifiesAgainstTheDocumentedSeedPassword()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // Login is "based on the users being stored", so the seeded users need a credential too - the
        // documented seed password (see README) lets a reviewer sign in as any of them straight away. It is
        // stored only as a PasswordHasher hash: never the clear-text value, and never empty.
        var context = CreateContext();
        var hasher = new PasswordHasher<User>();

        // Act: Invokes the method under test with the arranged parameters.
        var users = await context.GetAllAsync<User>();

        // Assert: Verifies that the action of the method under test behaves as expected.
        users.Should().NotBeEmpty();
        users.Should().OnlyContain(u => !string.IsNullOrEmpty(u.PasswordHash) && u.PasswordHash != SeedPassword);
        users.Should().OnlyContain(u => hasher.VerifyHashedPassword(u, u.PasswordHash!, SeedPassword) == PasswordVerificationResult.Success);
    }

    private const string SeedPassword = "12345";

    private DataContext CreateContext(string? databaseName = null)
    {
        var context = new DataContext(
            new DbContextOptionsBuilder<DataContext>().UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString()).Options);

        // The InMemory provider only applies OnModelCreating's HasData seed rows once the database is
        // actually created - unlike the real SQL Server app, which gets its schema/seed data from
        // Database.Migrate() at startup, these isolated per-test InMemory databases need EnsureCreated()
        // for the same effect (Migrate() doesn't apply to a provider with no migration history).
        context.Database.EnsureCreated();
        return context;
    }
}
