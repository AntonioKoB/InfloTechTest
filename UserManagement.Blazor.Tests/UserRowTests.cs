using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
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
        Services.AddSingleton(_poller.Object);
        Services.AddSingleton(_snackbar.Object);

        _poller.Setup(p => p.WaitForOutcomeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => Completed(id));
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
        var cut = RenderExpanded(user);

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
        var cut = RenderExpanded(user);
        cut.Find("#edit-button").Click();

        // Act
        cut.Find("#cancel-button").Click();

        // Assert
        _usersApi.Verify(a => a.UpdateUserAsync(It.IsAny<long>(), It.IsAny<UpdateUserRequest>()), Times.Never);
        cut.FindAll("input").Should().BeEmpty();
    }

    [Fact]
    public async Task EditMode_ClickSave_MustUpdateWaitForOutcomeAndRaiseOnSaved()
    {
        // Arrange
        var user = SetupUser();
        var accepted = Accepted();
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>())).ReturnsAsync(accepted);
        UserDto? savedArg = null;
        var cut = RenderExpanded(user, p => p.Add(x => x.OnSaved, saved => savedArg = saved));
        cut.Find("#edit-button").Click();
        cut.Find("#input-forename").Change("Renamed");

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        _usersApi.Verify(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>()), Times.Once);
        _poller.Verify(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>()), Times.Once);
        savedArg.Should().NotBeNull();
        savedArg!.Id.Should().Be(user.Id);
        savedArg.Forename.Should().Be("Renamed");
        savedArg.Surname.Should().Be(user.Surname);
        cut.Markup.Should().Contain("Renamed");
        cut.FindAll("input").Should().BeEmpty();
    }

    [Fact]
    public async Task EditMode_ClickSave_WhilePending_MustShowTheSavingStateAndDisableTheButtons()
    {
        // Arrange
        var user = SetupUser();
        var accepted = Accepted();
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>())).ReturnsAsync(accepted);
        var pending = new TaskCompletionSource<CommandStatusResponse>();
        _poller.Setup(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>())).Returns(pending.Task);
        var cut = RenderExpanded(user);
        cut.Find("#edit-button").Click();

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
    public async Task EditMode_SaveWhenTheCommandFails_MustShowTheFailureAndStayInEditMode()
    {
        // Arrange
        var user = SetupUser();
        var accepted = Accepted();
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>())).ReturnsAsync(accepted);
        _poller.Setup(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failed(accepted.CommandId, "A user with email 'taken@example.com' already exists."));
        var wasSaved = false;
        var cut = RenderExpanded(user, p => p.Add(x => x.OnSaved, _ => wasSaved = true));
        cut.Find("#edit-button").Click();

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        cut.Find("#email-error").TextContent.Should().Contain("already exists");
        cut.FindAll("input").Should().NotBeEmpty();
        wasSaved.Should().BeFalse();
        cut.FindAll("#saving-indicator").Should().BeEmpty();
    }

    [Fact]
    public async Task EditMode_SaveWhenTheCommandTimesOut_MustShowTheTimeoutErrorAndStayInEditMode()
    {
        // Arrange
        var user = SetupUser();
        var accepted = Accepted();
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>())).ReturnsAsync(accepted);
        _poller.Setup(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Command was still pending after 30 seconds."));
        var cut = RenderExpanded(user);
        cut.Find("#edit-button").Click();

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        cut.Find("#save-error").TextContent.Should().Contain("did not confirm");
        cut.FindAll("input").Should().NotBeEmpty();
        cut.Find("#save-button").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public async Task EditMode_PasswordInputStartsBlank_AndSavingWithoutTypingOneMustSendNullPassword()
    {
        // Arrange
        var user = SetupUser();
        UpdateUserRequest? sentRequest = null;
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>()))
            .Callback<long, UpdateUserRequest>((_, r) => sentRequest = r)
            .ReturnsAsync(Accepted());
        var cut = RenderExpanded(user);
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
        UpdateUserRequest? sentRequest = null;
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>()))
            .Callback<long, UpdateUserRequest>((_, r) => sentRequest = r)
            .ReturnsAsync(Accepted());
        var cut = RenderExpanded(user);
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
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>())).ReturnsAsync(Accepted());
        var cut = RenderExpanded(user);
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
        var conflict = await CreateEmailConflictException();
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>())).ThrowsAsync(conflict);
        var wasSaved = false;
        var cut = RenderExpanded(user, p => p.Add(x => x.OnSaved, _ => wasSaved = true));
        cut.Find("#edit-button").Click();

        // Act
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());

        // Assert
        cut.Find("#email-error").TextContent.Should().Contain("already exists");
        wasSaved.Should().BeFalse();
    }

    [Fact]
    public async Task ClickDelete_MustDeleteWaitForOutcomeAndRaiseOnDeleted()
    {
        // Arrange
        var user = SetupUser();
        var accepted = Accepted();
        _usersApi.Setup(a => a.DeleteUserAsync(user.Id)).ReturnsAsync(accepted);
        var wasDeleted = false;
        var cut = RenderExpanded(user, p => p.Add(x => x.OnDeleted, () => wasDeleted = true));

        // Act
        await cut.InvokeAsync(() => cut.Find("#delete-button").Click());

        // Assert
        _usersApi.Verify(a => a.DeleteUserAsync(user.Id), Times.Once);
        _poller.Verify(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>()), Times.Once);
        wasDeleted.Should().BeTrue();
        _snackbar.Verify(s => s.Add(It.IsAny<string>(), Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ClickDelete_WhilePending_MustShowTheDeletingStateAndDisableTheButtons()
    {
        // Arrange
        var user = SetupUser();
        var accepted = Accepted();
        _usersApi.Setup(a => a.DeleteUserAsync(user.Id)).ReturnsAsync(accepted);
        var pending = new TaskCompletionSource<CommandStatusResponse>();
        _poller.Setup(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>())).Returns(pending.Task);
        var cut = RenderExpanded(user);

        // Act
        await cut.InvokeAsync(() => cut.Find("#delete-button").Click());

        // Assert
        cut.Find("#deleting-indicator").Should().NotBeNull();
        cut.Find("#delete-button").HasAttribute("disabled").Should().BeTrue();
        cut.Find("#edit-button").HasAttribute("disabled").Should().BeTrue();

        pending.SetResult(Completed(accepted.CommandId));
        cut.WaitForAssertion(() => cut.FindAll("#deleting-indicator").Should().BeEmpty());
    }

    [Fact]
    public async Task ClickDelete_WhenTheCommandFails_MustShowTheErrorAndNotRaiseOnDeleted()
    {
        // Arrange
        var user = SetupUser();
        var accepted = Accepted();
        _usersApi.Setup(a => a.DeleteUserAsync(user.Id)).ReturnsAsync(accepted);
        _poller.Setup(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failed(accepted.CommandId, "Something went wrong while deleting."));
        var wasDeleted = false;
        var cut = RenderExpanded(user, p => p.Add(x => x.OnDeleted, () => wasDeleted = true));

        // Act
        await cut.InvokeAsync(() => cut.Find("#delete-button").Click());

        // Assert
        cut.Find("#delete-error").TextContent.Should().Contain("Something went wrong");
        wasDeleted.Should().BeFalse();
        cut.FindAll("#deleting-indicator").Should().BeEmpty();
        cut.Find("#delete-button").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public async Task ClickDelete_WhenTheCommandTimesOut_MustShowTheTimeoutErrorAndNotRaiseOnDeleted()
    {
        // Arrange
        var user = SetupUser();
        var accepted = Accepted();
        _usersApi.Setup(a => a.DeleteUserAsync(user.Id)).ReturnsAsync(accepted);
        _poller.Setup(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Command was still pending after 30 seconds."));
        var wasDeleted = false;
        var cut = RenderExpanded(user, p => p.Add(x => x.OnDeleted, () => wasDeleted = true));

        // Act
        await cut.InvokeAsync(() => cut.Find("#delete-button").Click());

        // Assert
        cut.Find("#delete-error").TextContent.Should().Contain("did not confirm");
        wasDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeWhileASaveIsPending_MustCancelThePoll()
    {
        // Arrange
        var user = SetupUser();
        var accepted = Accepted();
        _usersApi.Setup(a => a.UpdateUserAsync(user.Id, It.IsAny<UpdateUserRequest>())).ReturnsAsync(accepted);
        var pending = new TaskCompletionSource<CommandStatusResponse>();
        CancellationToken pollToken = default;
        _poller.Setup(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => pollToken = token)
            .Returns(pending.Task);
        var cut = RenderExpanded(user);
        cut.Find("#edit-button").Click();
        await cut.InvokeAsync(() => cut.Find("#save-button").Click());
        pollToken.CanBeCanceled.Should().BeTrue();

        // Act
        cut.Instance.Dispose();

        // Assert
        pollToken.IsCancellationRequested.Should().BeTrue();
        pending.SetCanceled(pollToken);
    }

    [Fact]
    public async Task DisposeWhileADeleteIsPending_MustCancelThePoll()
    {
        // Arrange
        var user = SetupUser();
        var accepted = Accepted();
        _usersApi.Setup(a => a.DeleteUserAsync(user.Id)).ReturnsAsync(accepted);
        var pending = new TaskCompletionSource<CommandStatusResponse>();
        CancellationToken pollToken = default;
        _poller.Setup(p => p.WaitForOutcomeAsync(accepted.CommandId, It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => pollToken = token)
            .Returns(pending.Task);
        var cut = RenderExpanded(user);
        await cut.InvokeAsync(() => cut.Find("#delete-button").Click());
        pollToken.CanBeCanceled.Should().BeTrue();

        // Act
        cut.Instance.Dispose();

        // Assert
        pollToken.IsCancellationRequested.Should().BeTrue();
        pending.SetCanceled(pollToken);
    }

    private IRenderedComponent<UserRow> RenderExpanded(UserDto user, Action<ComponentParameterCollectionBuilder<UserRow>>? configure = null)
    {
        _usersApi.Setup(a => a.GetUserByIdAsync(user.Id, true)).ReturnsAsync(user);
        _usersApi.Setup(a => a.GetUserLogsAsync(user.Id)).ReturnsAsync([]);
        var cut = Render<UserRow>(p =>
        {
            p.Add(x => x.RowKey, Guid.NewGuid()).Add(x => x.User, user);
            configure?.Invoke(p);
        });
        cut.Find("#row-header").Click();
        return cut;
    }

    private static CommandAcceptedResponse Accepted() => new() { CommandId = Guid.NewGuid() };

    private static CommandStatusResponse Completed(Guid commandId) => new() { CommandId = commandId, State = CommandState.Completed, UserId = 1 };

    private static CommandStatusResponse Failed(Guid commandId, string error) => new() { CommandId = commandId, State = CommandState.Failed, Error = error };

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
    private readonly Mock<ICommandPoller> _poller = new();
    private readonly Mock<ISnackbar> _snackbar = new();
}
