using UserManagement.Api.Contracts.Logs;

namespace UserManagement.Blazor.Components.Logs;

public static class UserLogActionExtensions
{
    public static string ToDisplayName(this UserLogAction action) => action switch
    {
        UserLogAction.LoggedIn => "Logged in",
        UserLogAction.LoggedOut => "Logged out",
        _ => action.ToString()
    };

    /// <summary>
    /// Suffix for the per-action badge colour class (log-action-created, log-action-loggedin, ...).
    /// </summary>
    public static string ToCssModifier(this UserLogAction action) => action.ToString().ToLowerInvariant();
}
