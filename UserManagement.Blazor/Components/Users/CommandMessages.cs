namespace UserManagement.Blazor.Components.Users;

/// <summary>
/// Shown when an accepted command never reports back within the poller's timeout.
/// </summary>
internal static class CommandMessages
{
    public const string Timeout = "The server accepted the request but did not confirm it in time. Refresh the list to check.";
}
