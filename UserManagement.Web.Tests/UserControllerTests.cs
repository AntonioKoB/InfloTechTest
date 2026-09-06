using System;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Web.Models.Users;
using UserManagement.WebMS.Controllers;

namespace UserManagement.Data.Tests;

public class UserControllerTests
{
    [Fact]
    public async Task List_WhenServiceReturnsUsers_ModelMustContainUsers()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        var users = SetupUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.List();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Model
            .Should().BeOfType<UserListViewModel>()
            .Which.Items.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task List_WhenFilterIsActive_MustReturnOnlyActiveUsers()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        var users = SetupActiveUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.List(UserListFilter.Active);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Model
            .Should().BeOfType<UserListViewModel>()
            .Which.Items.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task List_WhenFilterIsNonActive_MustReturnOnlyNonActiveUsers()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        var users = SetupNonActiveUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.List(UserListFilter.NonActive);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Model
            .Should().BeOfType<UserListViewModel>()
            .Which.Items.Should().BeEquivalentTo(users);
    }

    private User[] SetupUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", bool isActive = true, DateOnly? dateOfBirth = null)
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

        _userService
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(users);

        return users;
    }

    private User[] SetupActiveUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", DateOnly? dateOfBirth = null)
    {
        var users = new[]
        {
            new User
            {
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = true,
                DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1)
            }
        };

        _userService
            .Setup(s => s.FilterByActiveAsync(true))
            .ReturnsAsync(users);

        return users;
    }

    private User[] SetupNonActiveUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", DateOnly? dateOfBirth = null)
    {
        var users = new[]
        {
            new User
            {
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = false,
                DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1)
            }
        };

        _userService
            .Setup(s => s.FilterByActiveAsync(false))
            .ReturnsAsync(users);

        return users;
    }

    private readonly Mock<IUserService> _userService = new();
    private UsersController CreateController() => new(_userService.Object);
}
