using Microsoft.AspNetCore.Components;

namespace UserManagement.Blazor.Components.Users;

public partial class UserFormFields
{
    [Parameter, EditorRequired] public UserFormModel Model { get; set; } = default!;

    /// <summary>
    /// Extra fields rendered after the profile fields. Add and Edit each add their own password field here.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
