using Microsoft.AspNetCore.Components;

namespace UserManagement.Blazor.Components.Pages;

public partial class LoginPage
{
    /// <summary>
    /// Set when the API rejected the credentials.
    /// </summary>
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }

    /// <summary>
    /// Where the user was heading; posted back so the login endpoint can return them there.
    /// </summary>
    [SupplyParameterFromQuery(Name = "ReturnUrl")] public string? ReturnUrl { get; set; }
}
