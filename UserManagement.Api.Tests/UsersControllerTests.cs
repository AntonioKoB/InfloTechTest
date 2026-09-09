using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Api.Contracts.Users;
using UserManagement.Api.Controllers;
using UserManagement.Models;
using UserManagement.Services.Domain.Exceptions;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Api.Tests;

public class UsersControllerTests
{
    [Fact]
    public async Task GetUsers_WhenFilterIsAll_MustReturnAllUsersFromService()
    {
        // Arrange
        var controller = CreateController();
        var users = SetupUsers();

        // Act
        var result = await controller.GetUsers();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(users, o => o.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetUsers_WhenFilterIsActive_MustReturnOnlyActiveUsers()
    {
        // Arrange
        var controller = CreateController();
        var users = SetupActiveUsers();

        // Act
        var result = await controller.GetUsers(UserListFilter.Active);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(users, o => o.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetUsers_WhenFilterIsNonActive_MustReturnOnlyNonActiveUsers()
    {
        // Arrange
        var controller = CreateController();
        var users = SetupNonActiveUsers();

        // Act
        var result = await controller.GetUsers(UserListFilter.NonActive);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(users, o => o.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetUsers_MustMapEachUserToUserDto()
    {
        // Arrange
        var controller = CreateController();
        var users = SetupUsers();

        // Act
        var result = await controller.GetUsers();

        // Assert
        var dtos = result.Result.Should().BeOfType<OkObjectResult>().Which.Value
            .Should().BeAssignableTo<IEnumerable<UserDto>>().Subject;
        dtos.Should().BeEquivalentTo(users, o => o.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetById_WhenUserExists_MustReturnOkWithUserDto()
    {
        // Arrange
        var controller = CreateController();
        var user = SetupUser();

        // Act
        var result = await controller.GetById(user.Id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(user, o => o.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetById_WhenUserDoesNotExist_MustReturnNotFound()
    {
        // Arrange
        var controller = CreateController();
        _userService.Setup(s => s.GetByIdAsync(It.IsAny<long>(), It.IsAny<bool>())).ReturnsAsync((User?)null);

        // Act
        var result = await controller.GetById(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_MustPassRecordAsViewedThroughToService()
    {
        // Arrange
        var controller = CreateController();
        var user = SetupUser();

        // Act
        await controller.GetById(user.Id, recordAsViewed: true);

        // Assert
        _userService.Verify(s => s.GetByIdAsync(user.Id, true), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenRecordAsViewedNotSpecified_MustDefaultToFalse()
    {
        // Arrange
        var controller = CreateController();
        var user = SetupUser();

        // Act
        await controller.GetById(user.Id);

        // Assert
        _userService.Verify(s => s.GetByIdAsync(user.Id, false), Times.Once);
    }

    [Fact]
    public async Task GetUserLogs_MustReturnGetForUserAsyncItemsMappedToDto()
    {
        // Arrange
        var controller = CreateController();
        var user = SetupUser();
        var logs = new[]
        {
            new UserLog { Id = 1, UserId = user.Id, Action = UserManagement.Models.UserLogAction.Created, Timestamp = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc) }
        };
        _userLogService.Setup(s => s.GetForUserAsync(user.Id)).ReturnsAsync(logs);

        // Act
        var result = await controller.GetUserLogs(user.Id);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<UserLogDto>>()
            .Which.Should().ContainSingle(l => l.Id == 1 && l.UserId == user.Id);
    }

    [Fact]
    public async Task Create_WhenValid_MustCallCreateAsyncWithMappedUser()
    {
        // Arrange
        var controller = CreateController();
        var request = new CreateUserRequest
        {
            Forename = "Brand New",
            Surname = "User",
            Email = "brandnewuser@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12),
            IsActive = true
        };

        // Act
        await controller.Create(request);

        // Assert
        _userService.Verify(s => s.CreateAsync(It.Is<User>(u =>
            u.Forename == request.Forename &&
            u.Surname == request.Surname &&
            u.Email == request.Email &&
            u.DateOfBirth == request.DateOfBirth &&
            u.IsActive == request.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Create_WhenValid_MustReturnCreatedAtActionWithUserDto()
    {
        // Arrange
        var controller = CreateController();
        var request = new CreateUserRequest
        {
            Forename = "Brand New",
            Surname = "User",
            Email = "brandnewuser@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12),
            IsActive = true
        };

        // Act
        var result = await controller.Create(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>()
            .Which.Value.Should().BeAssignableTo<UserDto>()
            .Which.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task Create_WhenEmailAlreadyExists_MustReturnValidationProblemWithEmailModelError()
    {
        // Arrange
        var controller = CreateController();
        var request = new CreateUserRequest
        {
            Forename = "Brand New",
            Surname = "User",
            Email = "existing@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12),
            IsActive = true
        };
        _userService.Setup(s => s.CreateAsync(It.IsAny<User>())).ThrowsAsync(new EmailAlreadyExistsException(request.Email));

        // Act
        var result = await controller.Create(request);

        // Assert
        result.Result.Should().BeAssignableTo<ObjectResult>()
            .Which.Value.Should().BeOfType<ValidationProblemDetails>()
            .Which.Errors.Should().ContainKey(nameof(CreateUserRequest.Email));
    }

    [Fact]
    public async Task Update_WhenUserExists_MustCallUpdateAsyncWithMergedUser()
    {
        // Arrange
        var controller = CreateController();
        SetupUser(id: 5, forename: "Existing");
        var request = new UpdateUserRequest
        {
            Forename = "Updated",
            Surname = "User",
            Email = "updated@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12),
            IsActive = true
        };

        // Act
        await controller.Update(5, request);

        // Assert
        _userService.Verify(s => s.UpdateAsync(It.Is<User>(u =>
            u.Id == 5 &&
            u.Forename == request.Forename &&
            u.Surname == request.Surname &&
            u.Email == request.Email &&
            u.DateOfBirth == request.DateOfBirth &&
            u.IsActive == request.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Update_WhenUserExists_MustReturnOkWithUpdatedUserDto()
    {
        // Arrange
        var controller = CreateController();
        SetupUser(id: 5, forename: "Existing");
        var request = new UpdateUserRequest
        {
            Forename = "Updated",
            Surname = "User",
            Email = "updated@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12),
            IsActive = true
        };

        // Act
        var result = await controller.Update(5, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<UserDto>()
            .Which.Forename.Should().Be("Updated");
    }

    [Fact]
    public async Task Update_WhenUserDoesNotExist_MustReturnNotFound()
    {
        // Arrange
        var controller = CreateController();
        _userService.Setup(s => s.GetByIdAsync(999, It.IsAny<bool>())).ReturnsAsync((User?)null);
        var request = new UpdateUserRequest
        {
            Forename = "X",
            Surname = "Y",
            Email = "x@example.com",
            DateOfBirth = new DateOnly(1990, 1, 1),
            IsActive = true
        };

        // Act
        var result = await controller.Update(999, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _userService.Verify(s => s.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenEmailAlreadyExistsOnAnotherUser_MustReturnValidationProblemWithEmailModelError()
    {
        // Arrange
        var controller = CreateController();
        SetupUser(id: 5, forename: "Existing");
        var request = new UpdateUserRequest
        {
            Forename = "Updated",
            Surname = "User",
            Email = "taken@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12),
            IsActive = true
        };
        _userService.Setup(s => s.UpdateAsync(It.IsAny<User>())).ThrowsAsync(new EmailAlreadyExistsException(request.Email));

        // Act
        var result = await controller.Update(5, request);

        // Assert
        result.Result.Should().BeAssignableTo<ObjectResult>()
            .Which.Value.Should().BeOfType<ValidationProblemDetails>()
            .Which.Errors.Should().ContainKey(nameof(UpdateUserRequest.Email));
    }

    [Fact]
    public async Task Update_WhenUserNoLongerExists_MustReturnNotFound()
    {
        // Arrange
        var controller = CreateController();
        SetupUser(id: 5, forename: "Existing");
        var request = new UpdateUserRequest
        {
            Forename = "Updated",
            Surname = "User",
            Email = "updated@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12),
            IsActive = true
        };
        _userService.Setup(s => s.UpdateAsync(It.IsAny<User>())).ThrowsAsync(new UserNoLongerExistsException(5));

        // Act
        var result = await controller.Update(5, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_MustCallDeleteAsyncWithId()
    {
        // Arrange
        var controller = CreateController();

        // Act
        await controller.Delete(5);

        // Assert
        _userService.Verify(s => s.DeleteAsync(5), Times.Once);
    }

    [Fact]
    public async Task Delete_MustReturnNoContentRegardlessOfWhetherUserExisted()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.Delete(999);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    private User SetupUser(long id = 1, string forename = "Johnny", string surname = "User", string email = "juser@example.com", bool isActive = true, DateOnly? dateOfBirth = null)
    {
        var user = new User
        {
            Id = id,
            Forename = forename,
            Surname = surname,
            Email = email,
            IsActive = isActive,
            DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1)
        };

        _userService.Setup(s => s.GetByIdAsync(user.Id, It.IsAny<bool>())).ReturnsAsync(user);

        return user;
    }

    private User[] SetupUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", DateOnly? dateOfBirth = null)
    {
        var users = new[]
        {
            new User
            {
                Id = 1,
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = true,
                DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1)
            }
        };

        _userService.Setup(s => s.GetAllAsync()).ReturnsAsync(users);

        return users;
    }

    private User[] SetupActiveUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", DateOnly? dateOfBirth = null)
    {
        var users = new[]
        {
            new User
            {
                Id = 1,
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = true,
                DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1)
            }
        };

        _userService.Setup(s => s.FilterByActiveAsync(true)).ReturnsAsync(users);

        return users;
    }

    private User[] SetupNonActiveUsers(string forename = "Jane", string surname = "User", string email = "juser2@example.com", DateOnly? dateOfBirth = null)
    {
        var users = new[]
        {
            new User
            {
                Id = 2,
                Forename = forename,
                Surname = surname,
                Email = email,
                IsActive = false,
                DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1)
            }
        };

        _userService.Setup(s => s.FilterByActiveAsync(false)).ReturnsAsync(users);

        return users;
    }

    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IUserLogService> _userLogService = new();
    private UsersController CreateController() => new(_userService.Object, _userLogService.Object);
}
