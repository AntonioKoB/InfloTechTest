using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
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
        // freshly-built User instance for the same row. If FirstOrDefaultAsync tracked its result, EF's
        // alternate-key tracker would reject attaching that second instance for the same Email - which is
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
    public async Task CreateAsync_WhenEmailAlreadyExists_MustThrow()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
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
        // EF Core's InMemory provider enforces alternate keys eagerly when the entity is tracked
        // (InvalidOperationException), rather than at SaveChanges like a real relational provider
        // would (DbUpdateException) - either way, the duplicate is rejected before it can persist.
        await act.Should().ThrowAsync<InvalidOperationException>();
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
        await context.DeleteAsync(entity);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await context.GetAllAsync<User>();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().NotContain(s => s.Email == entity.Email);
    }

    private DataContext CreateContext() => new(Guid.NewGuid().ToString());
}
