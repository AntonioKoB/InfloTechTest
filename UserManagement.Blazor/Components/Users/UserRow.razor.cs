using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using MudBlazor;
using Refit;
using UserManagement.Api.Contracts.Commands;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Api.Contracts.Users;
using UserManagement.Blazor.Api;

namespace UserManagement.Blazor.Components.Users;

public partial class UserRow : IDisposable
{
    [Parameter, EditorRequired] public Guid RowKey { get; set; }
    [Parameter, EditorRequired] public UserDto User { get; set; } = default!;
    [Parameter] public EventCallback<UserDto> OnSaved { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }

    [Inject] private IUsersApi UsersApi { get; set; } = default!;
    [Inject] private ICommandPoller Poller { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    // Cancelled when the component is disposed, so a closed circuit does not keep a poll alive for the rest
    // of the poller's timeout.
    private readonly CancellationTokenSource _disposal = new();

    private bool _expanded;
    private bool _editing;
    private bool _saving;
    private bool _deleting;
    private string? _emailError;
    private string? _saveError;
    private string? _deleteError;
    private IReadOnlyList<UserLogDto> _activityLogs = [];
    private EditUserModel _editModel = new();

    private string Initials => $"{FirstLetter(User.Forename)}{FirstLetter(User.Surname)}";

    private static string FirstLetter(string value) => string.IsNullOrEmpty(value) ? "" : value[..1].ToUpperInvariant();

    private async Task ToggleExpand()
    {
        if (!_expanded)
        {
            User = await UsersApi.GetUserByIdAsync(User.Id, recordAsViewed: true);
            _activityLogs = await UsersApi.GetUserLogsAsync(User.Id);
        }

        _expanded = !_expanded;
    }

    private void StartEdit()
    {
        _editModel = ToEditModel(User);
        _emailError = null;
        _saveError = null;
        _editing = true;
    }

    private void Cancel()
    {
        _editing = false;
        _emailError = null;
        _saveError = null;
    }

    private async Task SaveAsync()
    {
        _emailError = null;
        _saveError = null;
        _saving = true;

        try
        {
            var accepted = await UsersApi.UpdateUserAsync(User.Id, ToUpdateRequest(_editModel));

            // The API only accepted the update; the typed values are confirmed once the worker reports Completed.
            var outcome = await Poller.WaitForOutcomeAsync(accepted.CommandId, _disposal.Token);
            if (outcome.State == CommandState.Failed)
            {
                _emailError = outcome.Error ?? "The user could not be saved.";
                return;
            }

            User = ToDto(_editModel, User.Id);
            _editing = false;
            Snackbar.Add("User saved successfully", Severity.Success);
            await OnSaved.InvokeAsync(User);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await ex.GetContentAsAsync<ValidationProblemDetails>();
            _emailError = problem?.Errors.TryGetValue("Email", out var emailErrors) == true
                ? emailErrors.FirstOrDefault()
                : "This email is already in use.";
        }
        catch (TimeoutException)
        {
            _saveError = CommandMessages.Timeout;
        }
        catch (OperationCanceledException)
        {
            // The component was disposed while waiting; there is nothing left to show.
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task DeleteAsync()
    {
        _deleteError = null;
        _deleting = true;

        try
        {
            var accepted = await UsersApi.DeleteUserAsync(User.Id);

            // The row goes only once the worker reports the user deleted.
            var outcome = await Poller.WaitForOutcomeAsync(accepted.CommandId, _disposal.Token);
            if (outcome.State == CommandState.Failed)
            {
                _deleteError = outcome.Error ?? "The user could not be deleted.";
                return;
            }

            Snackbar.Add("User deleted", Severity.Success);
            await OnDeleted.InvokeAsync();
        }
        catch (TimeoutException)
        {
            _deleteError = CommandMessages.Timeout;
        }
        catch (OperationCanceledException)
        {
            // The component was disposed while waiting; there is nothing left to show.
        }
        finally
        {
            _deleting = false;
        }
    }

    public void Dispose()
    {
        if (!_disposal.IsCancellationRequested)
            _disposal.Cancel();
        _disposal.Dispose();
    }

    private static UpdateUserRequest ToUpdateRequest(EditUserModel model) => new()
    {
        Forename = model.Forename,
        Surname = model.Surname,
        Email = model.Email,
        DateOfBirth = model.DateOfBirth,
        IsActive = model.IsActive,
        Password = string.IsNullOrEmpty(model.Password) ? null : model.Password
    };

    private static UserDto ToDto(EditUserModel model, long id) => new()
    {
        Id = id,
        Forename = model.Forename,
        Surname = model.Surname,
        Email = model.Email,
        DateOfBirth = model.DateOfBirth ?? default,
        IsActive = model.IsActive
    };

    private static EditUserModel ToEditModel(UserDto user) => new()
    {
        Forename = user.Forename,
        Surname = user.Surname,
        Email = user.Email,
        DateOfBirth = user.DateOfBirth == default ? null : user.DateOfBirth,
        IsActive = user.IsActive
    };
}
