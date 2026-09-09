using System.ComponentModel.DataAnnotations;

namespace UserManagement.Blazor.Components.Users;

public class UserFormModel
{
    [Required] public string Forename { get; set; } = "";
    [Required] public string Surname { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public DateOnly? DateOfBirth { get; set; }
    public bool IsActive { get; set; } = true;
}
