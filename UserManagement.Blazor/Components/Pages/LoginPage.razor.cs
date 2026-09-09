using Microsoft.AspNetCore.Components;

namespace UserManagement.Blazor.Components.Pages;

public partial class LoginPage
{
    /// <summary>
    /// Set by the login endpoint when the API rejected the credentials.
    /// </summary>
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }
}
