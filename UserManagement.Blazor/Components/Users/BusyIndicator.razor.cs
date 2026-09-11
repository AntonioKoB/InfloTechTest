using Microsoft.AspNetCore.Components;

namespace UserManagement.Blazor.Components.Users;

/// <summary>
/// Spinner with a label, shown while a command waits for its outcome; the id tells the saving and deleting
/// states apart.
/// </summary>
public partial class BusyIndicator
{
    [Parameter, EditorRequired] public string Id { get; set; } = "";
    [Parameter, EditorRequired] public string Text { get; set; } = "";
}
