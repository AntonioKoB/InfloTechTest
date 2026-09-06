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

    private DataContext CreateContext() => new();
}
