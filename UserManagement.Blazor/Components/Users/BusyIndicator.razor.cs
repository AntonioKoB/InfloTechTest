using Microsoft.AspNetCore.Components;

namespace UserManagement.Blazor.Components.Users;

/// <summary>
/// A small spinner with a label, shown while an accepted command is waiting for its outcome. The id is what
/// the tests (and anyone reading the DOM) use to tell the saving and deleting states apart.
/// </summary>
public partial class BusyIndicator
{
    [Parameter, EditorRequired] public string Id { get; set; } = "";
    [Parameter, EditorRequired] public string Text { get; set; } = "";
}
