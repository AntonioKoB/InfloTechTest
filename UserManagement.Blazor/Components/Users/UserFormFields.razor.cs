using Microsoft.AspNetCore.Components;

namespace UserManagement.Blazor.Components.Users;

public partial class UserFormFields
{
    [Parameter, EditorRequired] public UserFormModel Model { get; set; } = default!;

    /// <summary>
    /// Extra fields rendered inside the same grid, after the profile fields and before the Active toggle.
    /// Add and Edit each contribute their own password field here, since it is required on one and optional
    /// on the other and binds to a different model on each.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
