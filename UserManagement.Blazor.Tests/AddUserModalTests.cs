using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Refit;
using UserManagement.Api.Contracts.Commands;
using UserManagement.Api.Contracts.Users;
using UserManagement.Blazor.Api;
using UserManagement.Blazor.Components.Users;

namespace UserManagement.Blazor.Tests;

public class AddUserModalTests : BunitContext
{
    public AddUserModalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(_usersApi.Object);
        Services.AddSingleton(_snackbar.Object);
    }

    [Fact]
    public void WhenNotVisible_MustRenderNothing()
    {
        // Act
        var cut = Render<AddUserModal>(p => p.Add(x => x.Visible, false));

        // Assert
        cut.FindAll("input").Should().BeEmpty();
    }

    [Fact]
    public void WhenVisible_MustShowBlankForm()
    {
        // Act
        var cut = Render<AddUserModal>(p => p.Add(x => x.Visible, true));

        // Assert
        cut.FindAll("input").Should().NotBeEmpty();
        cut.Find("#save-button").Should().NotBeNull();
        cut.Find("#cancel-button").Should().NotBeNull();
    }

    [Fact]
    public async Task ClickSave_MustCallCreateUserAsyncAndRaiseOnSavedWithSnackbar()
    {
        // Arrange
        // The API accepts the create and returns a command id, not the user: there is no created row to hand
        // back, so OnSaved carries nothing and the page decides how to refresh.
        _usersApi.Setup(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>())).ReturnsAsync(Accepted());
        var wasSaved = false;
        var cut = Render<AddUserModal>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.OnSaved, () => wasSaved = true));
        FillForm(cut);

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        _usersApi.Verify(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>()), Times.Once);
        wasSaved.Should().BeTrue();
        _snackbar.Verify(s => s.Add(It.IsAny<string>(), Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void ClickCancel_MustRaiseOnCancelledWithoutCallingApi()
    {
        // Arrange
        var wasCancelled = false;
        var cut = Render<AddUserModal>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.OnCancelled, () => wasCancelled = true));

        // Act
        cut.Find("#cancel-button").Click();

        // Assert
        wasCancelled.Should().BeTrue();
        _usersApi.Verify(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>()), Times.Never);
    }

    [Fact]
    public async Task SaveWhenApiReturnsEmailConflict_MustShowFieldErrorAndNotRaiseOnSaved()
    {
        // Arrange
        var conflict = await CreateEmailConflictException();
        _usersApi.Setup(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>())).ThrowsAsync(conflict);
        var wasSaved = false;
        var cut = Render<AddUserModal>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.OnSaved, () => wasSaved = true));
        FillForm(cut);

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        cut.Find("#email-error").TextContent.Should().Contain("already exists");
        wasSaved.Should().BeFalse();
    }

    [Fact]
    public void WhenVisible_MustShowAPasswordInput()
    {
        // Act
        var cut = Render<AddUserModal>(p => p.Add(x => x.Visible, true));

        // Assert
        cut.Find("#input-password").GetAttribute("type").Should().Be("password");
    }

    [Fact]
    public async Task ClickSave_MustSendThePasswordInTheCreateUserRequest()
    {
        // Arrange
        CreateUserRequest? sentRequest = null;
        _usersApi.Setup(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>()))
            .Callback<CreateUserRequest>(r => sentRequest = r)
            .ReturnsAsync(Accepted());
        var cut = Render<AddUserModal>(p => p.Add(x => x.Visible, true));
        FillForm(cut, password: "12345");

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        sentRequest.Should().NotBeNull();
        sentRequest!.Password.Should().Be("12345");
    }

    [Fact]
    public async Task ClickSaveWithBlankPassword_MustNotCallCreateUserAsync()
    {
        // Arrange
        // A new user cannot exist without a credential - the form must refuse to submit rather than let the
        // API reject it after the round trip.
        var cut = Render<AddUserModal>(p => p.Add(x => x.Visible, true));
        FillForm(cut, password: null);

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        _usersApi.Verify(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>()), Times.Never);
    }

    private static void FillForm(IRenderedComponent<AddUserModal> cut, string? password = "12345")
    {
        cut.Find("#input-forename").Change("New");
        cut.Find("#input-surname").Change("User");
        cut.Find("#input-email").Change("new@example.com");
        cut.Find("#input-dob").Change("2000-01-01");
        if (password is not null)
        {
            cut.Find("#input-password").Change(password);
        }
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
        var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost/api/users");
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        return await ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    private readonly Mock<IUsersApi> _usersApi = new();
    private readonly Mock<ISnackbar> _snackbar = new();
}
