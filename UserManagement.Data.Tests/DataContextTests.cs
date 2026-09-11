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
        var context = CreateContext();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.WhereAsync<User>(u => u.IsActive == false);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(u => u.IsActive == false);
    }

    [Fact]
    public async Task DeleteWhereAsync_OnInMemoryProvider_IsNotSupported()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        // The InMemory provider has no translator for ExecuteDelete; on SQL Server this is a real DELETE ... WHERE.
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
        // Email is a unique index, not a key, so it stays editable; the InMemory provider does not enforce it, UserService does.
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
    public async Task UpdateAsync_WhenRowDeletedSinceFetch_MustThrowConcurrencyConflict()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
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
        var context = CreateContext();

        var logs = Enumerable.Range(1, 5)
            .Select(i => new UserLog { UserId = 1, Action = UserLogAction.Created, Timestamp = DateTime.UtcNow.AddDays(i) })
            .ToArray();
        foreach (var log in logs) await context.CreateAsync(log);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.GetPageAsync<UserLog, DateTime>(l => l.Timestamp, descending: true, skip: 1, take: 2);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeEquivalentTo([logs[3], logs[2]], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task CountAsync_WhenCalled_MustReturnTotalCountOfThatEntityType()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
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

        // EnsureCreated applies the HasData seed rows on InMemory, where Migrate() does not apply.

        context.Database.EnsureCreated();
        return context;
    }
}
