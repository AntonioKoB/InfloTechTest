using Microsoft.AspNetCore.Components;

namespace UserManagement.Blazor.Components.Pages;

public partial class LoginPage
{
    /// <summary>
    /// Set by the login endpoint when the API rejected the credentials.
    /// </summary>
    [SupplyParameterFromQuery(Name = "error")] public string? Error { get; set; }

    /// <summary>
    /// Where the user was heading when the cookie middleware sent them here; posted back so the login
    /// endpoint can return them there.
    /// </summary>
    [SupplyParameterFromQuery(Name = "ReturnUrl")] public string? ReturnUrl { get; set; }
}
