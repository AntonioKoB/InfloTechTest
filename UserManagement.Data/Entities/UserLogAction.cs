namespace UserManagement.Models;

public enum UserLogAction
{
    Created,
    Viewed,
    Updated,
    Deleted,
    // Session boundaries: no snapshot, no diff. Append only; the column stores the numeric value.
    LoggedIn,
    LoggedOut
}
