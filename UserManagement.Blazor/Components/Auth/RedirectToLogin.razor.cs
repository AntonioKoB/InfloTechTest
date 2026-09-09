using Microsoft.AspNetCore.Components;

namespace UserManagement.Blazor.Components.Auth;

public partial class RedirectToLogin
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized() => Navigation.NavigateTo("/login");
}
