namespace UserManagement.Models;

public enum UserLogAction
{
    Created,
    Viewed,
    Updated,
    Deleted,
    // Session boundaries: no snapshot, no diff - just who and when. Appended, never reordered, because the
    // column stores the numeric value.
    LoggedIn,
    LoggedOut
}
