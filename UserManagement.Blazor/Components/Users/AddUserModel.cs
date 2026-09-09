using System.ComponentModel.DataAnnotations;

namespace UserManagement.Blazor.Components.Users;

public class AddUserModel : UserFormModel
{
    [Required] public string Password { get; set; } = "";
}
