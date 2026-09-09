using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using MudBlazor;
using Refit;
using UserManagement.Api.Contracts.Logs;
using UserManagement.Api.Contracts.Users;
using UserManagement.Blazor.Api;

namespace UserManagement.Blazor.Components.Users;

public partial class UserRow
{
    [Parameter, EditorRequired] public Guid RowKey { get; set; }
    [Parameter, EditorRequired] public UserDto User { get; set; } = default!;
    [Parameter] public EventCallback<UserDto> OnSaved { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }

    [Inject] private IUsersApi UsersApi { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private bool _expanded;
    private bool _editing;
    private string? _emailError;
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
        _editing = true;
    }

    private void Cancel()
    {
        _editing = false;
        _emailError = null;
    }

    private async Task SaveAsync()
    {
        _emailError = null;

        try
        {
            var saved = await UsersApi.UpdateUserAsync(User.Id, ToUpdateRequest(_editModel));

            User = saved;
            _editing = false;
            Snackbar.Add("User saved successfully", Severity.Success);
            await OnSaved.InvokeAsync(saved);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await ex.GetContentAsAsync<ValidationProblemDetails>();
            _emailError = problem?.Errors.TryGetValue("Email", out var emailErrors) == true
                ? emailErrors.FirstOrDefault()
                : "This email is already in use.";
        }
    }

    private async Task DeleteAsync()
    {
        await UsersApi.DeleteUserAsync(User.Id);
        Snackbar.Add("User deleted", Severity.Success);
        await OnDeleted.InvokeAsync();
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

    private static EditUserModel ToEditModel(UserDto user) => new()
    {
        Forename = user.Forename,
        Surname = user.Surname,
        Email = user.Email,
        DateOfBirth = user.DateOfBirth == default ? null : user.DateOfBirth,
        IsActive = user.IsActive
    };
}
