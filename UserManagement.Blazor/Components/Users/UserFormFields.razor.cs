using Microsoft.AspNetCore.Components;

namespace UserManagement.Blazor.Components.Users;

public partial class UserFormFields
{
    [Parameter, EditorRequired] public UserEditModel Model { get; set; } = default!;
}
