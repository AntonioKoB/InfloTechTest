using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
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
        Services.AddSingleton(_poller.Object);
        Services.AddSingleton(_snackbar.Object);

        // Unless a test says otherwise, every accepted command completes.
        _poller.Setup(p => p.WaitForOutcomeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => Completed(id));
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
    public async Task ClickSave_MustCallCreateUserAsyncWaitForTheOutcomeAndRaiseOnSavedWithSnackbar()
    {
        // Arrange
        // The API accepts the create and returns a command id; OnSaved fires once the poller reports
        // Completed, so the page reloads a list that already holds the new user.
        var accepted = Accepted();
        _usersApi.Setup(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>())).ReturnsAsync(accepted);
        var wasSaved = false;
        var cut = Render<AddUserModal>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.OnSaved, () => wasSaved = true));
        FillForm(cut);

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        _usersApi.Verify(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>()), Times.Once);
        _poller.Verify(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>()), Times.Once);
        wasSaved.Should().BeTrue();
        _snackbar.Verify(s => s.Add(It.IsAny<string>(), Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        cut.FindAll("#saving-indicator").Should().BeEmpty();
    }

    [Fact]
    public async Task ClickSave_WhilePending_MustShowTheSavingStateAndDisableTheButtons()
    {
        // Arrange
        var accepted = Accepted();
        _usersApi.Setup(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>())).ReturnsAsync(accepted);
        var pending = new TaskCompletionSource<CommandStatusResponse>();
        _poller.Setup(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>())).Returns(pending.Task);
        var cut = Render<AddUserModal>(p => p.Add(x => x.Visible, true));
        FillForm(cut);

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        cut.Find("#saving-indicator").Should().NotBeNull();
        cut.Find("#save-button").HasAttribute("disabled").Should().BeTrue();
        cut.Find("#cancel-button").HasAttribute("disabled").Should().BeTrue();

        pending.SetResult(Completed(accepted.CommandId));
        cut.WaitForAssertion(() => cut.FindAll("#saving-indicator").Should().BeEmpty());
    }

    [Fact]
    public async Task SaveWhenTheCommandFails_MustShowTheFailureInTheEmailErrorSlotAndNotRaiseOnSaved()
    {
        // Arrange
        // The duplicate email is caught by the worker, not by the request: it arrives as a Failed status
        // and lands in the same slot the form already uses for an email error.
        var accepted = Accepted();
        _usersApi.Setup(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>())).ReturnsAsync(accepted);
        _poller.Setup(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failed(accepted.CommandId, "A user with email 'new@example.com' already exists."));
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
        cut.FindAll("input").Should().NotBeEmpty();
        cut.FindAll("#saving-indicator").Should().BeEmpty();
        _snackbar.Verify(s => s.Add(It.IsAny<string>(), Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveWhenTheCommandTimesOut_MustShowTheTimeoutErrorAndNotRaiseOnSaved()
    {
        // Arrange
        var accepted = Accepted();
        _usersApi.Setup(a => a.CreateUserAsync(It.IsAny<CreateUserRequest>())).ReturnsAsync(accepted);
        _poller.Setup(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Command was still pending after 30 seconds."));
        var wasSaved = false;
        var cut = Render<AddUserModal>(p => p
            .Add(x => x.Visible, true)
            .Add(x => x.OnSaved, () => wasSaved = true));
        FillForm(cut);

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        cut.Find("#save-error").TextContent.Should().Contain("did not confirm");
        wasSaved.Should().BeFalse();
        cut.FindAll("#saving-indicator").Should().BeEmpty();
        cut.Find("#save-button").HasAttribute("disabled").Should().BeFalse();
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

    private static CommandStatusResponse Completed(Guid commandId) => new() { CommandId = commandId, State = CommandState.Completed, UserId = 5 };

    private static CommandStatusResponse Failed(Guid commandId, string error) => new() { CommandId = commandId, State = CommandState.Failed, Error = error };

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
    private readonly Mock<ICommandPoller> _poller = new();
    private readonly Mock<ISnackbar> _snackbar = new();
}
