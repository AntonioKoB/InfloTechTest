using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using MudBlazor;
using Refit;
using UserManagement.Api.Contracts.Commands;
using UserManagement.Api.Contracts.Users;
using UserManagement.Blazor.Api;

namespace UserManagement.Blazor.Components.Users;

public partial class AddUserModal : IDisposable
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    [Inject] private IUsersApi UsersApi { get; set; } = default!;
    [Inject] private ICommandPoller Poller { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    // Cancelled on dispose so a closed circuit does not keep polling.
    private readonly CancellationTokenSource _disposal = new();

    private AddUserModel _model = new();
    private bool _saving;
    private string? _emailError;
    private string? _saveError;

    private async Task SaveAsync()
    {
        _emailError = null;
        _saveError = null;
        _saving = true;

        try
        {
            var accepted = await UsersApi.CreateUserAsync(new CreateUserRequest
            {
                Forename = _model.Forename,
                Surname = _model.Surname,
                Email = _model.Email,
                DateOfBirth = _model.DateOfBirth,
                IsActive = _model.IsActive,
                Password = _model.Password
            });

            var outcome = await Poller.WaitForOutcomeAsync(accepted.CommandId, _disposal.Token);
            if (outcome.State == CommandState.Failed)
            {
                _emailError = outcome.Error ?? "The user could not be created.";
                return;
            }

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
        catch (TimeoutException)
        {
            _saveError = CommandMessages.Timeout;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task Cancel()
    {
        _model = new();
        _emailError = null;
        _saveError = null;
        await OnCancelled.InvokeAsync();
    }

    public void Dispose()
    {
        if (!_disposal.IsCancellationRequested)
            _disposal.Cancel();
        _disposal.Dispose();
    }
}
