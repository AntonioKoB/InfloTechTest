using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Refit;
using UserManagement.Api.Contracts.Commands;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Api.Contracts.Users;
using UserManagement.Blazor.Api;
using UserManagement.Blazor.Components.Users;

namespace UserManagement.Blazor.Tests;

public class UserRowTests : BunitContext
{
    public UserRowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(_usersApi.Object);
        Services.AddSingleton(_snackbar.Object);
    }

    [Fact]
    public void CollapsedRow_MustShowOnlyIdAndFullName()
    {
        // Arrange
        var user = SetupUser();

        // Act
        var cut = Render<UserRow>(p => p
            .Add(x => x.RowKey, Guid.NewGuid())
            .Add(x => x.User, user));

        // Assert
        cut.Markup.Should().Contain(user.Id.ToString())
            .And.Contain($"{user.Forename} {user.Surname}");
        cut.Markup.Should().NotContain(user.Email);
    }

    [Fact]
    public void ClickCollapsedRow_MustExpandAndCallGetByIdWithRecordAsViewedTrue()
    {
        // Arrange
        var user = SetupUser();
        _usersApi.Setup(a => a.GetUserByIdAsync(user.Id, true)).ReturnsAsync(user);
        _usersApi.Setup(a => a.GetUserLogsAsync(user.Id)).ReturnsAsync([]);
        var cut = Render<UserRow>(p => p
            .Add(x => x.RowKey, Guid.NewGuid())
            .Add(x => x.User, user));

        // Act
        cut.Find("#row-header").Click();

        // Assert
        _usersApi.Verify(a => a.GetUserByIdAsync(user.Id, true), Times.Once);
    }

    [Fact]
    public void ExpandedRow_MustShowSurnameEmailDateOfBirthIsActiveAndActivityLog()
    {
        // Arrange
        var user = SetupUser();
        _usersApi.Setup(a => a.GetUserByIdAsync(user.Id, true)).ReturnsAsync(user);
        var logs = new[] { new UserLogDto { Id = 1, UserId = user.Id, Action = UserLogAction.Created, Timestamp = new DateTime(2026, 9, 1) } };
        _usersApi.Setup(a => a.GetUserLogsAsync(user.Id)).ReturnsAsync(logs);
        var cut = Render<UserRow>(p => p
            .Add(x => x.RowKey, Guid.NewGuid())
            .Add(x => x.User, user));

        // Act
        cut.Find("#row-header").Click();

        // Assert
        cut.Markup.Should().Contain(user.Surname)
            .And.Contain(user.Email)
            .And.Contain(user.DateOfBirth.ToString("dd MMM yyyy"))
            .And.Contain("Created");
    }

    [Fact]
    public void ExpandedRow_ClickEdit_MustSwitchToEditableInputs()
    {
        // Arrange
        var user = SetupUser();
        _usersApi.Setup(a => a.GetUserByIdAsync(user.Id, true)).ReturnsAsync(user);
        _usersApi.Setup(a => a.GetUserLogsAsync(user.Id)).ReturnsAsync([]);
        var cut = Render<UserRow>(p => p
            .Add(x => x.RowKey, Guid.NewGuid())
            .Add(x => x.User, user));
        cut.Find("#row-header").Click();

        // Act
        cut.Find("#edit-button").Click();

        // Assert
        cut.FindAll("input").Should().NotBeEmpty();
        cut.Find("#save-button").Should().NotBeNull();
    }

    [Fact]
    public void EditMode_ClickCancelOnExistingRow_MustRevertWithoutCallingUpdate()
    {
        // Arrange
        var user = SetupUser();
        _usersApi.Setup(a => a.GetUserByIdAsync(user.Id, true)).ReturnsAsync(user);
        _usersApi.Setup(a => a.GetUserLogsAsync(user.Id)).ReturnsAsync([]);
        var cut = Render<UserRow>(p => p
            .Add(x => x.RowKey, Guid.NewGuid())
            .Add(x => x.User, user));
        cut.Find("#row-header").Click();
        cut.Find("#edit-button").Click();

        // Act
        cut.Find("#cancel-button").Click();

        // Assert
        _usersApi.Verify(a => a.UpdateUserAsync(It.IsAny<long>(), It.IsAny<UpdateUserRequest>()), Times.Never);
        cut.FindAll("input").Should().BeEmpty();
    }

    [Fact]
    public async Task EditMode_ClickSave_MustCallUpdateUserAsyncAndRaiseOnSavedWithTheEditedValues()
    {
        // Arrange
        // The API accepts the update and returns a command id, not the saved user. The row shows what was
        // typed, so the values the user just entered are what OnSaved carries and what the row renders.
        var user = SetupUser();
        _usersApi.Setup(a => a.GetUserByIdAsync(user.Id, true)).ReturnsAsync(user);
        _usersApi.Setup(a => a.GetUserLogsAsync(user.Id)).ReturnsAsync([]);
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>())).ReturnsAsync(Accepted());
        UserDto? savedArg = null;
        var cut = Render<UserRow>(p => p
            .Add(x => x.RowKey, Guid.NewGuid())
            .Add(x => x.User, user)
            .Add(x => x.OnSaved, saved => savedArg = saved));
        cut.Find("#row-header").Click();
        cut.Find("#edit-button").Click();
        cut.Find("#input-forename").Change("Renamed");

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        _usersApi.Verify(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>()), Times.Once);
        savedArg.Should().NotBeNull();
        savedArg!.Id.Should().Be(user.Id);
        savedArg.Forename.Should().Be("Renamed");
        savedArg.Surname.Should().Be(user.Surname);
        cut.Markup.Should().Contain("Renamed");
    }

    [Fact]
    public async Task EditMode_PasswordInputStartsBlank_AndSavingWithoutTypingOneMustSendNullPassword()
    {
        // Arrange
        // The stored hash is never shown or pre-filled. Leaving the field blank means "keep the current
        // password", which the API expects as a null Password on the update request.
        var user = SetupUser();
        _usersApi.Setup(a => a.GetUserByIdAsync(user.Id, true)).ReturnsAsync(user);
        _usersApi.Setup(a => a.GetUserLogsAsync(user.Id)).ReturnsAsync([]);
        UpdateUserRequest? sentRequest = null;
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>()))
            .Callback<long, UpdateUserRequest>((_, r) => sentRequest = r)
            .ReturnsAsync(Accepted());
        var cut = Render<UserRow>(p => p
            .Add(x => x.RowKey, Guid.NewGuid())
            .Add(x => x.User, user));
        cut.Find("#row-header").Click();
        cut.Find("#edit-button").Click();
        var passwordInput = cut.Find("#input-password");
        passwordInput.GetAttribute("type").Should().Be("password");
        passwordInput.GetAttribute("value").Should().BeNullOrEmpty();

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        sentRequest.Should().NotBeNull();
        sentRequest!.Password.Should().BeNull();
    }

    [Fact]
    public async Task EditMode_ClickSaveAfterTypingANewPassword_MustSendItInTheUpdateUserRequest()
    {
        // Arrange
        var user = SetupUser();
        _usersApi.Setup(a => a.GetUserByIdAsync(user.Id, true)).ReturnsAsync(user);
        _usersApi.Setup(a => a.GetUserLogsAsync(user.Id)).ReturnsAsync([]);
        UpdateUserRequest? sentRequest = null;
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>()))
            .Callback<long, UpdateUserRequest>((_, r) => sentRequest = r)
            .ReturnsAsync(Accepted());
        var cut = Render<UserRow>(p => p
            .Add(x => x.RowKey, Guid.NewGuid())
            .Add(x => x.User, user));
        cut.Find("#row-header").Click();
        cut.Find("#edit-button").Click();
        cut.Find("#input-password").Change("new-secret");

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        sentRequest.Should().NotBeNull();
        sentRequest!.Password.Should().Be("new-secret");
    }

    [Fact]
    public async Task EditMode_ClickSave_MustShowSuccessSnackbar()
    {
        // Arrange
        var user = SetupUser();
        _usersApi.Setup(a => a.GetUserByIdAsync(user.Id, true)).ReturnsAsync(user);
        _usersApi.Setup(a => a.GetUserLogsAsync(user.Id)).ReturnsAsync([]);
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>())).ReturnsAsync(Accepted());
        var cut = Render<UserRow>(p => p
            .Add(x => x.RowKey, Guid.NewGuid())
            .Add(x => x.User, user));
        cut.Find("#row-header").Click();
        cut.Find("#edit-button").Click();

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        _snackbar.Verify(s => s.Add(It.IsAny<string>(), Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EditMode_SaveWhenApiReturnsEmailConflict_MustShowFieldErrorAndNotRaiseOnSaved()
    {
        // Arrange
        var user = SetupUser();
        _usersApi.Setup(a => a.GetUserByIdAsync(user.Id, true)).ReturnsAsync(user);
        _usersApi.Setup(a => a.GetUserLogsAsync(user.Id)).ReturnsAsync([]);
        var conflict = await CreateEmailConflictException();
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>())).ThrowsAsync(conflict);
        var wasSaved = false;
        var cut = Render<UserRow>(p => p
            .Add(x => x.RowKey, Guid.NewGuid())
            .Add(x => x.User, user)
            .Add(x => x.OnSaved, _ => wasSaved = true));
        cut.Find("#row-header").Click();
        cut.Find("#edit-button").Click();

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        cut.Find("#email-error").TextContent.Should().Contain("already exists");
        wasSaved.Should().BeFalse();
    }

    [Fact]
    public async Task ClickDelete_MustCallDeleteUserAsyncAndRaiseOnDeletedWithSnackbar()
    {
        // Arrange
        var user = SetupUser();
        _usersApi.Setup(a => a.GetUserByIdAsync(user.Id, true)).ReturnsAsync(user);
        _usersApi.Setup(a => a.GetUserLogsAsync(user.Id)).ReturnsAsync([]);
        _usersApi.Setup(a => a.DeleteUserAsync(user.Id)).Returns(Task.CompletedTask);
        var wasDeleted = false;
        var cut = Render<UserRow>(p => p
            .Add(x => x.RowKey, Guid.NewGuid())
            .Add(x => x.User, user)
            .Add(x => x.OnDeleted, () => wasDeleted = true));
        cut.Find("#row-header").Click();

        // Act
        await cut.InvokeAsync(() => cut.Find("#delete-button").Click());

        // Assert
        _usersApi.Verify(a => a.DeleteUserAsync(user.Id), Times.Once);
        wasDeleted.Should().BeTrue();
        _snackbar.Verify(s => s.Add(It.IsAny<string>(), Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    private static CommandAcceptedResponse Accepted() => new() { CommandId = Guid.NewGuid() };

    private static async Task<ApiException> CreateEmailConflictException()
    {
        var body = JsonSerializer.Serialize(new
        {
            title = "One or more validation errors occurred.",
            status = 400,
            errors = new Dictionary<string, string[]> { ["Email"] = ["A user with this email already exists."] }
        });
        var request = new HttpRequestMessage(HttpMethod.Put, "https://localhost/api/users/1");
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        return await ApiException.Create(request, HttpMethod.Put, response, new RefitSettings());
    }

    private UserDto SetupUser(long id = 1, string forename = "Johnny", string surname = "User", string email = "juser@example.com", bool isActive = true)
        => new()
        {
            Id = id,
            Forename = forename,
            Surname = surname,
            Email = email,
            IsActive = isActive,
            DateOfBirth = new DateOnly(1990, 1, 1)
        };

    private readonly Mock<IUsersApi> _usersApi = new();
    private readonly Mock<ISnackbar> _snackbar = new();
}
