using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Domain.Implementations;

namespace UserManagement.Data.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task GetAllAsync_WhenContextReturnsEntities_MustReturnSameEntities()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var users = SetupUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetAllAsync();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeSameAs(users);
    }

    [Fact]
    public async Task FilterByActiveAsync_WhenIsActiveTrue_MustReturnOnlyActiveUsers()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var (activeUsers, _) = SetupMixedUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.FilterByActiveAsync(true);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeEquivalentTo(activeUsers);
    }

    [Fact]
    public async Task FilterByActiveAsync_WhenIsActiveFalse_MustReturnOnlyNonActiveUsers()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var (_, nonActiveUsers) = SetupMixedUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.FilterByActiveAsync(false);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeEquivalentTo(nonActiveUsers);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_MustReturnMatchingUser()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var users = SetupUsers();
        var expected = users.First();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetByIdAsync(expected.Id);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_MustReturnNull()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        SetupUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetByIdAsync(999);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeNull();
    }

    private IEnumerable<User> SetupUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", bool isActive = true, DateOnly? dateOfBirth = null)
    {
        var users = new[]
        {
            new User
            {
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = isActive,
                DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1)
            }
        };

        _dataContext
            .Setup(s => s.GetAllAsync<User>())
            .ReturnsAsync(users);

        return users;
    }

    private (User[] ActiveUsers, User[] NonActiveUsers) SetupMixedUsers()
    {
        var activeUser = new User { Forename = "Johnny", Surname = "User", Email = "juser@example.com", IsActive = true, DateOfBirth = new DateOnly(1990, 1, 1) };
        var nonActiveUser = new User { Forename = "Jane", Surname = "NonUser", Email = "jnonuser@example.com", IsActive = false, DateOfBirth = new DateOnly(1985, 6, 15) };

        var users = new[] { activeUser, nonActiveUser };

        _dataContext
            .Setup(s => s.GetAllAsync<User>())
            .ReturnsAsync(users);

        return (new[] { activeUser }, new[] { nonActiveUser });
    }

    private readonly Mock<IDataContext> _dataContext = new();
    private UserService CreateService() => new(_dataContext.Object);
}
