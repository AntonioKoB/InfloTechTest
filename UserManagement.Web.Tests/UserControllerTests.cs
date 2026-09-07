using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Models;
using UserManagement.Services.Domain.Exceptions;
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

    [Fact]
    public async Task View_WhenUserExists_MustReturnViewResultWithUser()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        var user = SetupUser();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.View(user.Id);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<UserViewModel>()
            .Which.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task View_WhenUserDoesNotExist_MustReturnUserNotFoundView()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        _userService
            .Setup(s => s.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((User?)null);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.View(999);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task Add_WhenModelStateIsInvalid_MustReturnViewWithModel()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        controller.ModelState.AddModelError("Email", "The Email field is required.");
        var model = new UserFormViewModel();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.Add(model);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeSameAs(model);
        _userService.Verify(s => s.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Add_WhenModelStateIsValid_MustCreateUserAndRedirectToList()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        var model = new UserFormViewModel
        {
            Forename = "Brand New",
            Surname = "User",
            Email = "brandnewuser@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12),
            IsActive = true
        };

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.Add(model);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _userService.Verify(s => s.CreateAsync(It.Is<User>(u =>
            u.Forename == model.Forename &&
            u.Surname == model.Surname &&
            u.Email == model.Email &&
            u.DateOfBirth == model.DateOfBirth &&
            u.IsActive == model.IsActive)), Times.Once);
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(UsersController.List));
    }

    [Fact]
    public async Task Add_WhenEmailAlreadyExists_MustReturnViewWithModelStateError()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        var model = new UserFormViewModel
        {
            Forename = "Brand New",
            Surname = "User",
            Email = "existing@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12),
            IsActive = true
        };
        _userService
            .Setup(s => s.CreateAsync(It.IsAny<User>()))
            .ThrowsAsync(new EmailAlreadyExistsException(model.Email));

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.Add(model);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeSameAs(model);
        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState[nameof(UserFormViewModel.Email)]!.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Edit_WhenUserExists_MustReturnViewResultWithPopulatedForm()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        var user = SetupUser();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.Edit(user.Id);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<UserFormViewModel>()
            .Which.Should().BeEquivalentTo(user, options => options.ExcludingMissingMembers());
    }

    [Fact]
    public async Task Edit_WhenUserDoesNotExist_MustReturnUserNotFoundView()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        _userService
            .Setup(s => s.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((User?)null);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.Edit(999);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task Edit_WhenSubmittingToNonExistentUser_MustReturnUserNotFoundView()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        _userService
            .Setup(s => s.GetByIdAsync(999))
            .ReturnsAsync((User?)null);
        var model = new UserFormViewModel
        {
            Forename = "Updated",
            Surname = "User",
            Email = "updated@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12),
            IsActive = true
        };

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.Edit(999, model);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("UserNotFound");
        _userService.Verify(s => s.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Edit_WhenModelStateIsInvalid_MustReturnViewWithModel()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        _userService
            .Setup(s => s.GetByIdAsync(5))
            .ReturnsAsync(new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) });
        controller.ModelState.AddModelError("Email", "The Email field is required.");
        var model = new UserFormViewModel();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.Edit(5, model);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeSameAs(model);
        _userService.Verify(s => s.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Edit_WhenModelStateIsValid_MustUpdateUserAndRedirectToList()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        _userService
            .Setup(s => s.GetByIdAsync(5))
            .ReturnsAsync(new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) });
        var model = new UserFormViewModel
        {
            Forename = "Updated",
            Surname = "User",
            Email = "updated@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12),
            IsActive = true
        };

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.Edit(5, model);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _userService.Verify(s => s.UpdateAsync(It.Is<User>(u =>
            u.Id == 5 &&
            u.Forename == model.Forename &&
            u.Surname == model.Surname &&
            u.Email == model.Email &&
            u.DateOfBirth == model.DateOfBirth &&
            u.IsActive == model.IsActive)), Times.Once);
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(UsersController.List));
    }

    [Fact]
    public async Task Edit_WhenEmailAlreadyExistsOnAnotherUser_MustReturnViewWithModelStateError()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        _userService
            .Setup(s => s.GetByIdAsync(5))
            .ReturnsAsync(new User { Id = 5, Forename = "Existing", Surname = "User", Email = "existing@example.com", DateOfBirth = new DateOnly(1990, 1, 1) });
        var model = new UserFormViewModel
        {
            Forename = "Updated",
            Surname = "User",
            Email = "taken@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12),
            IsActive = true
        };
        _userService
            .Setup(s => s.UpdateAsync(It.IsAny<User>()))
            .ThrowsAsync(new EmailAlreadyExistsException(model.Email));

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.Edit(5, model);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeSameAs(model);
        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState[nameof(UserFormViewModel.Email)]!.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Delete_WhenUserExists_MustReturnViewResultWithUser()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        var user = SetupUser();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.Delete(user.Id);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<UserViewModel>()
            .Which.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task Delete_WhenUserDoesNotExist_MustReturnUserNotFoundView()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        _userService
            .Setup(s => s.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((User?)null);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.Delete(999);

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task DeleteConfirmed_MustDeleteUserAndReturnOk()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.DeleteConfirmed(5);

        // Assert: Verifies that the action of the method under test behaves as expected.
        _userService.Verify(s => s.DeleteAsync(5), Times.Once);
        result.Should().BeOfType<OkResult>();
    }

    private User SetupUser(string forename = "Johnny", string surname = "User", string email = "juser@example.com", bool isActive = true, DateOnly? dateOfBirth = null)
    {
        var user = new User
        {
            Forename = forename,
            Surname = surname,
            Email = email,
            IsActive = isActive,
            DateOfBirth = dateOfBirth ?? new DateOnly(1990, 1, 1)
        };

        _userService
            .Setup(s => s.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

        return user;
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
