using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using MudBlazor;
using Refit;
using UserManagement.Api.Contracts.Users;
using UserManagement.Blazor.Api;

namespace UserManagement.Blazor.Components.Users;

public partial class AddUserModal
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    [Inject] private IUsersApi UsersApi { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private AddUserModel _model = new();
    private string? _emailError;

    private async Task SaveAsync()
    {
        _emailError = null;

        try
        {
            await UsersApi.CreateUserAsync(new CreateUserRequest
            {
                Forename = _model.Forename,
                Surname = _model.Surname,
                Email = _model.Email,
                DateOfBirth = _model.DateOfBirth,
                IsActive = _model.IsActive,
                Password = _model.Password
            });

            _model = new();
            Snackbar.Add("User created successfully", Severity.Success);
            await OnSaved.InvokeAsync();
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await ex.GetContentAsAsync<ValidationProblemDetails>();
            _emailError = problem?.Errors.TryGetValue("Email", out var emailErrors) == true
                ? emailErrors.FirstOrDefault()
                : "This email is already in use.";
        }
    }

    private async Task Cancel()
    {
        _model = new();
        _emailError = null;
        await OnCancelled.InvokeAsync();
    }
}
