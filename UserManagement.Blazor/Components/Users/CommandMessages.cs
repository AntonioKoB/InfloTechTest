namespace UserManagement.Blazor.Components.Users;

/// <summary>
/// The one message the user sees when an accepted command never reports back within the poller's timeout.
/// The poller's own exception text is technical; this is what the form shows.
/// </summary>
internal static class CommandMessages
{
    public const string Timeout = "The server accepted the request but did not confirm it in time. Refresh the list to check.";
}
