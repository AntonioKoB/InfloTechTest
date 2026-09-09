using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using UserManagement.Api.Contracts.Users;
using UserManagement.Blazor.Api;
using UserManagement.Blazor.Components.Pages;
using UserManagement.Blazor.Components.Users;

namespace UserManagement.Blazor.Tests;

public class UsersPageTests : BunitContext
{
    public UsersPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(_usersApi.Object);
        Services.AddSingleton(_snackbar.Object);
    }

    [Fact]
    public void OnInitialized_MustLoadAllUsers()
    {
        // Arrange
        var users = new[] { SetupUser(1), SetupUser(2) };
        _usersApi.Setup(a => a.GetUsersAsync(UserListFilter.All)).ReturnsAsync(users);

        // Act
        var cut = Render<UsersPage>();

        // Assert
        _usersApi.Verify(a => a.GetUsersAsync(UserListFilter.All), Times.Once);
        cut.FindComponents<UserRow>().Should().HaveCount(2);
    }

    [Fact]
    public void ClickFilterButtons_MustReloadWithMatchingFilter()
    {
        // Arrange
        _usersApi.Setup(a => a.GetUsersAsync(It.IsAny<UserListFilter>())).ReturnsAsync([]);
        var cut = Render<UsersPage>();

        // Act
        cut.Find("#filter-active").Click();

        // Assert
        _usersApi.Verify(a => a.GetUsersAsync(UserListFilter.Active), Times.Once);
    }

    [Fact]
    public void ClickAddNewUser_MustOpenTheAddModal()
    {
        // Arrange
        _usersApi.Setup(a => a.GetUsersAsync(UserListFilter.All)).ReturnsAsync([]);
        var cut = Render<UsersPage>();
        cut.FindComponent<AddUserModal>().Instance.Visible.Should().BeFalse();

        // Act
        cut.Find("#add-user-button").Click();

        // Assert
        cut.FindComponent<AddUserModal>().Instance.Visible.Should().BeTrue();
    }

    [Fact]
    public async Task ModalOnSaved_MustAppendCreatedUserAndCloseModal()
    {
        // Arrange
        _usersApi.Setup(a => a.GetUsersAsync(UserListFilter.All)).ReturnsAsync([]);
        var cut = Render<UsersPage>();
        cut.Find("#add-user-button").Click();
        var modal = cut.FindComponent<AddUserModal>();
        var created = SetupUser(5, "Brand New");

        // Act
        await cut.InvokeAsync(() => modal.Instance.OnSaved.InvokeAsync(created));

        // Assert
        cut.FindComponents<UserRow>().Should().ContainSingle(r => r.Instance.User.Forename == "Brand New");
        cut.FindComponent<AddUserModal>().Instance.Visible.Should().BeFalse();
    }

    [Fact]
    public async Task ModalOnCancelled_MustCloseModalWithoutAddingRow()
    {
        // Arrange
        _usersApi.Setup(a => a.GetUsersAsync(UserListFilter.All)).ReturnsAsync([]);
        var cut = Render<UsersPage>();
        cut.Find("#add-user-button").Click();
        var modal = cut.FindComponent<AddUserModal>();

        // Act
        await cut.InvokeAsync(() => modal.Instance.OnCancelled.InvokeAsync());

        // Assert
        cut.FindComponents<UserRow>().Should().BeEmpty();
        cut.FindComponent<AddUserModal>().Instance.Visible.Should().BeFalse();
    }

    [Fact]
    public async Task RowOnSaved_MustReplaceMatchingRowInList()
    {
        // Arrange
        var user = SetupUser(1, "Original");
        _usersApi.Setup(a => a.GetUsersAsync(UserListFilter.All)).ReturnsAsync([user]);
        var cut = Render<UsersPage>();
        var row = cut.FindComponent<UserRow>();
        var updated = new UserDto { Id = 1, Forename = "Updated", Surname = user.Surname, Email = user.Email, IsActive = user.IsActive, DateOfBirth = user.DateOfBirth };

        // Act
        await cut.InvokeAsync(() => row.Instance.OnSaved.InvokeAsync(updated));

        // Assert
        cut.FindComponent<UserRow>().Instance.User.Forename.Should().Be("Updated");
    }

    [Fact]
    public async Task RowOnDeleted_MustRemoveMatchingRowFromList()
    {
        // Arrange
        var user = SetupUser(1);
        _usersApi.Setup(a => a.GetUsersAsync(UserListFilter.All)).ReturnsAsync([user]);
        var cut = Render<UsersPage>();
        var row = cut.FindComponent<UserRow>();

        // Act
        await cut.InvokeAsync(() => row.Instance.OnDeleted.InvokeAsync());

        // Assert
        cut.FindComponents<UserRow>().Should().BeEmpty();
    }

    private static UserDto SetupUser(long id, string forename = "Johnny")
        => new()
        {
            Id = id,
            Forename = forename,
            Surname = "User",
            Email = $"user{id}@example.com",
            IsActive = true,
            DateOfBirth = new DateOnly(1990, 1, 1)
        };

    private readonly Mock<IUsersApi> _usersApi = new();
    private readonly Mock<ISnackbar> _snackbar = new();
}
